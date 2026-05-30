# Distributed Chunks

## Overview

The game universe is divided into a grid of square chunks. Each chunk is an independent `GoLChunkGrain` actor. Chunks exchange edge and corner cell states during every simulation step so that cells on boundaries follow the correct Conway rules.

## Chunk Addressing

Chunks are addressed by their `(chunkX, chunkY)` position. The grain key is `"chunkX_chunkY"`, e.g. `"0_0"`, `"1_2"`. The `GoLUniverseGrain` creates and references chunk grains via:

```csharp
IGoLChunkGrain GetChunkGrain(int x, int y) =>
    GrainFactory.GetGrain<IGoLChunkGrain>($"{x}_{y}");
```

## Step Algorithm

For each generation, `GoLUniverseGrain.StepUniverse()` fans out to all chunk grains in parallel:

```csharp
var stepTasks = ChunkCoords().Select(c => GetChunkGrain(c.x, c.y).Advance()).ToList();
await Task.WhenAll(stepTasks);
```

Inside each `GoLChunkGrain.Advance()`:

1. **Fetch neighbor edges** — parallel async RPC calls to all 8 neighboring chunks for their exposed edges/corners.
2. **Compute next state** — apply Conway rules to every cell. Edge cells use the fetched neighbor data for out-of-bounds lookups.
3. **Swap buffers** — `IsAlive ← IsAliveNext` atomically.
4. **Persist state** — write to the `ChunkMemory` grain storage provider.

## Inter-Chunk Communication API

All read methods are marked `[AlwaysInterleave]` so multiple chunks can read each other's edges concurrently without queuing:

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

Edge arrays are indexed left-to-right (top/bottom) or top-to-bottom (left/right).

## Boundary Wrapping

Chunks on the universe edge have no neighbors beyond the boundary; those edge cells treat out-of-universe neighbors as dead (no wrapping).

## Initialization

`GoLUniverseGrain.InitUniverse()` (called by `POST /api/init`):
1. Saves dimensions to persisted state.
2. Creates/activates all chunk grains and calls `InitChunk(x, y, size, liveDensity)` on each in parallel.
3. Builds an in-memory grid snapshot from all chunks.

`POST /api/reinit` reuses the same grain instances if dimensions haven't changed (just re-randomizes cells), avoiding re-activation overhead.

## Grain Placement

`GoLChunkGrain` uses `[ActivationCountBasedPlacement]`, which directs Orleans to place new activations on the silo with the fewest existing activations. For a 2×2 grid on a 4-silo cluster, each chunk lands on a different silo.
