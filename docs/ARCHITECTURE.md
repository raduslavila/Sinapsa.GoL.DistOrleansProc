# Architecture

## Overview

Conway's Game of Life runs as an **Orleans actor cluster** on .NET 9. The game grid is partitioned into square chunks — each managed by a `GoLChunkGrain` — coordinated by a single `GoLUniverseGrain` that lives on exactly one silo across the whole cluster.

## Grain Design

### GoLUniverseGrain (`key = "universe"`)

Cluster-singleton coordinator grain. Because Orleans guarantees a single activation per (type, key) pair, this grain is the one authoritative source of truth for:

- Universe dimensions (`ChunksX`, `ChunksY`, `ChunkSize`)
- Current generation counter
- Grid snapshots (in activation memory; rebuilt from chunk grains on reactivation)

Persistence: `[StorageProvider(ProviderName = "ChunkMemory")]` — in-memory by default.

The `GoLService` application layer is a thin proxy that resolves the grain by its fixed key:

```csharp
private IGoLUniverseGrain Universe =>
    _grainFactory.GetGrain<IGoLUniverseGrain>("universe");
```

### GoLChunkGrain

One grain per chunk of the grid. Each grain owns a `ChunkSize × ChunkSize` boolean cell array and communicates edge states with its 8 neighbors during each `Advance()` call.

**Placement**: `[ActivationCountBasedPlacement]` — overrides the global `PreferLocalPlacement` default, so chunk grains distribute across silos rather than all landing on the same one.

Edge and corner methods are `[AlwaysInterleave]` so multiple chunks can read each other's edges concurrently:

```csharp
[AlwaysInterleave] Task<bool[]> GetTopEdge();
[AlwaysInterleave] Task<bool[]> GetBottomEdge();
[AlwaysInterleave] Task<bool[]> GetLeftEdge();
[AlwaysInterleave] Task<bool[]> GetRightEdge();
[AlwaysInterleave] Task<bool>   GetTopLeftCorner();
[AlwaysInterleave] Task<bool>   GetTopRightCorner();
[AlwaysInterleave] Task<bool>   GetBottomLeftCorner();
[AlwaysInterleave] Task<bool>   GetBottomRightCorner();
```

Persistence: same `ChunkMemory` provider.

## Request Flow (`POST /api/step`)

```
HTTP client
  ↓ UniverseController → GoLService.RunUniverseStep()
  ↓ GoLUniverseGrain("universe").StepUniverse()     [single silo]
        Task.WhenAll ─┬─ GoLChunkGrain("0_0").Advance()  [silo A]
                     ├─ GoLChunkGrain("0_1").Advance()  [silo B]
                     ├─ GoLChunkGrain("1_0").Advance()  [silo C]
                     └─ GoLChunkGrain("1_1").Advance()  [silo D]
        each chunk fetches edges from its 8 neighbors
  ↑ Ok()
```

## Project Layer Map

| Project | Role |
|---------|------|
| `Sinapsa.GoL.DistOrleansProc` | ASP.NET Core host, Orleans silo bootstrap, REST controllers |
| `Sinapsa.GoL.DistOrleansProc.Domain` | `GoLService` (proxy), DI wiring, `SiloHostBuilderExtensions` |
| `Sinapsa.GoL.DistOrleansProc.GrainInterfaces` | `IGoLUniverseGrain`, `IGoLChunkGrain`, DTOs, state models |
| `Sinapsa.GoL.DistOrleansProc.Grains` | `GoLUniverseGrain`, `GoLChunkGrain` implementations |
| `Sinapsa.GoL.DistOrleansProc.Orleans.Core` | Generic grain infrastructure helpers |

## Clustering

**Local development**: `UseLocalhost` clustering (single silo, in-process).

**Kubernetes**: `Orleans.Clustering.Kubernetes 10.0.1` with CRD-based membership.
- CRDs: `silos.orleans.dot.net`, `clusterversions.orleans.dot.net` (applied from `k8s/crds.yaml`)
- ServiceAccount with RBAC to read/write CRDs (applied from `k8s/rbac.yaml`)
- `ClusterId` must be RFC 1123-compliant (lowercase only) — currently `k8s-gol`
- Pod IP injected via K8s downward API (`POD_IP` env var) and used as `AdvertisedIPAddress`

## Ports

| Port | Purpose |
|------|---------|
| 5050 | HTTP API + Orleans Dashboard (at `/dashboard`) |
| 11111 | Client gateway (silo ↔ client) |
| 30000 | Silo-to-silo communication |

The Orleans Dashboard is served by `MapOrleansDashboard()` on the same Kestrel port as the API — it is **not** a standalone server on a separate port.

## Storage

Currently uses `AddMemoryGrainStorage("ChunkMemory")` — in-memory, cleared on silo restart. The `[StorageProvider]` attributes on both grains reference this provider. Replacing with Redis or another durable store requires only changing the provider registration in `Program.cs`.
