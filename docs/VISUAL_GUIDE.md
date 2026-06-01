# Visual Guide

## Chunk Grid Layout (2×2 example)

```
Universe: 64×64 cells total (2 chunks × 32 cells per chunk)

+------------------+------------------+
|   chunk_0_0      |   chunk_0_1      |
|   (GoLChunkGrain |   (GoLChunkGrain |
|    key = "0_0")  |    key = "0_1")  |
|   32×32 cells    |   32×32 cells    |
+------------------+------------------+
|   chunk_1_0      |   chunk_1_1      |
|   (GoLChunkGrain |   (GoLChunkGrain |
|    key = "1_0")  |    key = "1_1")  |
|   32×32 cells    |   32×32 cells    |
+------------------+------------------+
```

Each chunk is a separate Orleans grain activation. With `[ActivationCountBasedPlacement]`, these are spread across available silos.

## Edge Communication During a Step

```
           chunk_0_0              chunk_0_1
         +----------+            +----------+
         | . . . . R|            |L . . . . |
         | . . . . R|  GetRight  |L . . . . |
         | . . . . R| ---------> |          |
         | . . . . R|            |          |
         |BBBBBBBBBB|            |          |
         +----------+            +----------+
              |                       |
              | GetBottom             |
              v                       |
         +----------+                 |
         |TTTTTTTTTT|  chunk_1_0      |
         | . . . . R|                 |
         | . . . . R|                 |
         | . . . . R|                 |
         | . . . . R|                 |
         +----------+                 |

  T = top edge  B = bottom edge  L = left edge  R = right edge
  Corners are fetched as single booleans.
```

All edge reads are `[AlwaysInterleave]`, so chunks within the same silo read each other's edges concurrently without head-of-line blocking.

## Cluster Topology (4 silos × 4 chunk grains)

```
+--------------------------------------------------------------+
|  Kubernetes namespace: gol                                   |
|                                                              |
|  gol-backend-pod-A (silo 10.244.3.4:30000)                  |
|  ┌────────────────────────────────────────────┐             |
|  │ GoLUniverseGrain("universe")  ← singleton  │             |
|  │ GoLChunkGrain("0_0")                        │             |
|  └────────────────────────────────────────────┘             |
|                                                              |
|  gol-backend-pod-B (silo 10.244.2.12:30000)                 |
|  ┌────────────────────────────────────────────┐             |
|  │ GoLChunkGrain("0_1")                        │             |
|  └────────────────────────────────────────────┘             |
|                                                              |
|  gol-backend-pod-C (silo 10.244.2.14:30000)                 |
|  ┌────────────────────────────────────────────┐             |
|  │ GoLChunkGrain("1_0")                        │             |
|  └────────────────────────────────────────────┘             |
|                                                              |
|  gol-backend-pod-D (silo 10.244.3.5:30000)                  |
|  ┌────────────────────────────────────────────┐             |
|  │ GoLChunkGrain("1_1")                        │             |
|  └────────────────────────────────────────────┘             |
|                                                              |
|  gol-frontend-pod (nginx + React build)                      |
+--------------------------------------------------------------+

  HTTP traffic arrives at any backend pod via the NodePort service.
  All pods share cluster state via Orleans' grain directory.
  GoLUniverseGrain always lives on exactly one silo.
```

## Request Routing

```
Browser / curl
    │
    │  http://localhost:30050
    ▼
Any backend pod (5050)
    │  GoLService.RunUniverseStep()
    ▼
GoLUniverseGrain("universe")   ← always on the same silo
    │  Task.WhenAll(Advance x N)
    ├──► GoLChunkGrain("0_0")  [local or remote RPC]
    ├──► GoLChunkGrain("0_1")  [local or remote RPC]
    ├──► GoLChunkGrain("1_0")  [local or remote RPC]
    └──► GoLChunkGrain("1_1")  [local or remote RPC]
         └── each fetches edges from its 8 neighbors
```
