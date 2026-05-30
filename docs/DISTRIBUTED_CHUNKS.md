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

## Overview

This document describes the distributed multi-chunk architecture implemented for Conway's Game of Life. The system now supports dividing the game universe into multiple chunks (grains) that can communicate with each other to process edge cells correctly, enabling true distributed processing across Orleans silos.

## Key Changes

### 1. **Inter-Chunk Communication**

Chunks can now communicate with their neighbors to exchange edge cell states. This is critical for properly calculating the Game of Life rules for cells on chunk boundaries.

#### New Interface Methods (IGoLChunkGrain)

```csharp
// Edge retrieval methods (marked with [AlwaysInterleave] for non-blocking reads)
Task<bool[]> GetTopEdge();
Task<bool[]> GetBottomEdge();
Task<bool[]> GetLeftEdge();
Task<bool[]> GetRightEdge();
Task<bool> GetTopLeftCorner();
Task<bool> GetTopRightCorner();
Task<bool> GetBottomLeftCorner();
Task<bool> GetBottomRightCorner();

// Neighbor configuration
Task SetNeighborChunks(
    string topChunkId, string bottomChunkId,
    string leftChunkId, string rightChunkId,
    string topLeftChunkId, string topRightChunkId,
    string bottomLeftChunkId, string bottomRightChunkId);

// New initialization method with chunk coordinates
Task InitChunk(int chunkX, int chunkY, int width, int height, double liveDensity);
```

### 2. **Grain State Extensions**

The `GoLChunkGrainState` now tracks neighbor chunk identifiers:

```csharp
public string TopChunkId { get; set; }
public string BottomChunkId { get; set; }
public string LeftChunkId { get; set; }
public string RightChunkId { get; set; }
public string TopLeftChunkId { get; set; }
public string TopRightChunkId { get; set; }
public string BottomLeftChunkId { get; set; }
public string BottomRightChunkId { get; set; }
```

### 3. **Enhanced GoLChunkGrain Implementation**

The grain now:
- Fetches neighbor edge states during `Advance()`
- Counts cross-chunk neighbors for edge cells
- Properly applies Game of Life rules across chunk boundaries

#### Algorithm Flow in `Advance()`:

1. **Fetch Neighbor Edges** - Parallel async calls to all neighboring chunks
2. **Process Each Cell** - Count internal neighbors + cross-chunk neighbors
3. **Apply Rules** - Standard Conway's Game of Life rules
4. **Update State** - Synchronize all cells to their next state
5. **Persist** - Save state to storage (memory now, Redis in the future)

### 4. **Multi-Chunk Service (GoLService)**

New methods for distributed universe management:

```csharp
// Initialize a grid of chunks
Task InitUniverse(int chunksX, int chunksY, int chunkWidth, int chunkHeight, double liveDensity);

// Run one generation across all chunks in parallel
Task RunUniverseStepDistributed();

// Get combined display of entire universe
Task<string> DisplayUniverseState();
```

#### Initialization Process:

1. **Create Chunks** - Initialize all chunks in parallel
2. **Configure Neighbors** - Set up neighbor references for each chunk
3. **Ready** - All chunks are now ready to simulate

The service maintains a grid structure:
- Chunks are identified as `chunk_{x}_{y}`
- Each chunk knows its position in the grid
- Edge chunks have null neighbor references where appropriate

### 5. **New API Endpoints**

Added `UniverseController` with endpoints:

- **POST /universe/init** - Initialize distributed universe
  - Query parameters: `chunksX`, `chunksY`, `chunkWidth`, `chunkHeight`, `liveDensity`
  - Example: `POST /universe/init?chunksX=4&chunksY=4&chunkWidth=32&chunkHeight=32`

- **POST /universe/step** - Advance all chunks by one generation
  - Returns ASCII visualization of entire universe

- **GET /universe/state** - Get current state without advancing
  - Returns ASCII visualization of entire universe

## Architecture Benefits

### Scalability
- **Horizontal Scaling**: Different chunks can run on different silos
- **Parallel Processing**: All chunks advance simultaneously
- **Load Distribution**: Large universes are automatically distributed

### Orleans Features Leveraged
- **Grain Activation**: Chunks are activated on-demand
- **Location Transparency**: Chunks can call each other regardless of silo location
- **AlwaysInterleave**: Edge reads don't block grain processing

### Performance Characteristics
- **Parallel Advancement**: All chunks advance in parallel using `Task.WhenAll()`
- **Minimal State Transfer**: Only edge data is exchanged between chunks
- **Async Communication**: Non-blocking inter-grain calls

## Usage Examples

### Example 1: Small 2x2 Grid

```bash
# Initialize 2x2 chunks, each 32x32 cells (total 64x64 universe)
curl -X POST "http://localhost:5050/universe/init?chunksX=2&chunksY=2&chunkWidth=32&chunkHeight=32&liveDensity=0.15"

# Run one generation
curl -X POST http://localhost:5050/universe/step

# View current state
curl http://localhost:5050/universe/state
```

### Example 2: Large 4x4 Grid

```bash
# Initialize 4x4 chunks, each 50x50 cells (total 200x200 universe)
curl -X POST "http://localhost:5050/universe/init?chunksX=4&chunksY=4&chunkWidth=50&chunkHeight=50&liveDensity=0.1"

# Run multiple generations
for i in {1..10}; do
  curl -X POST http://localhost:5050/universe/step
done
```

## Chunk Communication Flow

### During Initialization:
```
GoLService.InitUniverse()
    ??> Create all chunks in parallel
    ??> Configure neighbor relationships
    ??> All chunks ready

chunk_0_0.SetNeighborChunks(...)
    ??> right: chunk_1_0
    ??> bottom: chunk_0_1
    ??> bottomRight: chunk_1_1
```

### During Advance:
```
GoLService.RunUniverseStepDistributed()
    ??> Task.WhenAll(all chunks.Advance())

chunk_0_0.Advance()
    ??> FetchNeighborEdges()
    ?   ??> chunk_1_0.GetLeftEdge()
    ?   ??> chunk_0_1.GetTopEdge()
    ?   ??> chunk_1_1.GetTopLeftCorner()
    ??> Process all cells (including edge cells)
    ??> WriteStateAsync()
```

## Edge Cases Handled

1. **Boundary Chunks**: Chunks at universe edges have null neighbors
2. **Corner Cells**: Special handling for cells that need diagonal chunk neighbors
3. **Concurrent Access**: `[AlwaysInterleave]` on edge reads prevents deadlocks
4. **State Consistency**: Each chunk reads neighbor edges at start of `Advance()`

## Cross-Chunk Neighbor Calculation

For a cell on the left edge of a chunk (x=0):
```
?????????????????????????????
?  chunk_0_0  ?  chunk_1_0  ?
?             ?             ?
?          [X]?[N]          ?  X = edge cell in chunk_0_0
?             ?             ?  N = neighbor in chunk_1_0
?????????????????????????????

CountCrossChunkNeighbors():
  if (isLeftEdge && edgeData.LeftEdge != null)
    count += edgeData.LeftEdge[y] ? 1 : 0
```

## Future: Redis Persistence (TODOs Added)

### Current TODOs in Code:

1. **GoLChunkGrain.cs**:
   ```csharp
   // TODO: Add Redis persistence support for distributed state management
   // Consider using [StorageProvider(ProviderName = "RedisGrainStorage")]
   ```

2. **GoLChunkGrain.Advance()**:
   ```csharp
   // TODO: When migrating to Redis, consider batching writes for better performance
   ```

3. **GoLService.cs**:
   ```csharp
   // TODO: When migrating to Redis, implement chunk state caching and distributed locking
   // to prevent race conditions across multiple silo instances
   ```

4. **GoLService.RunUniverseStepDistributed()**:
   ```csharp
   // TODO: When using Redis, consider implementing optimistic concurrency control
   // to handle concurrent updates across distributed silos
   ```

### Redis Migration Steps:

1. **Add Redis Storage Provider**:
   ```csharp
   siloBuilder.AddRedisGrainStorage("RedisChunkStorage", options =>
   {
       options.ConnectionString = "localhost:6379";
       options.DatabaseNumber = 0;
   });
   ```

2. **Update Grain Attribute**:
   ```csharp
   [StorageProvider(ProviderName = "RedisChunkStorage")]
   public class GoLChunkGrain : Grain<GoLChunkGrainState>, IGoLChunkGrain
   ```

3. **Consider Caching**:
   - Cache edge data to reduce Redis reads
   - Implement TTL for inactive chunks
   - Use Redis pub/sub for chunk coordination

4. **Implement Distributed Locking**:
   - Use RedLock or similar for cross-silo coordination
   - Prevent concurrent modifications during universe steps
   - Ensure consistency across distributed silos

## Testing the Distributed System

### Same Silo Testing (Current):
All chunks run on the same silo but in separate grains:
```bash
# Check Orleans Dashboard
http://localhost:8000/dashboard

# Look for multiple grain activations:
# - chunk_0_0
# - chunk_0_1
# - chunk_1_0
# - chunk_1_1
```

### Multi-Silo Testing (Future):
1. Deploy multiple silo instances
2. Configure clustering (not localhost)
3. Initialize universe
4. Observe chunk distribution across silos in dashboard

## Observability

### Dashboard Metrics to Monitor:
- **Grain Activations**: Number of chunk grains active
- **Method Calls**: Frequency of `Advance()`, `GetEdge()` calls
- **Throughput**: Generations per second
- **Latency**: Time per `Advance()` call

### Logging:
The `UniverseController` logs:
- Universe initialization parameters
- Step execution

## Performance Considerations

### Optimization Opportunities:
1. **Batch Edge Fetching**: Fetch all neighbor edges in single call
2. **Edge Caching**: Cache edge data for one generation
3. **Lazy Activation**: Only activate chunks with live cells
4. **Compression**: Compress sparse grids before storage
5. **Async Patterns**: Use Orleans Streams for real-time updates

### Scalability Limits:
- **Network Latency**: Cross-silo calls add latency
- **State Size**: Large chunks may hit serialization limits
- **Coordination**: Synchronizing thousands of chunks requires optimization

## Comparison: Single vs. Multi-Chunk

| Aspect | Single Chunk | Multi-Chunk (New) |
|--------|--------------|-------------------|
| Grid Size | Fixed (32x32) | Configurable (NxM chunks) |
| Scalability | Single grain | Distributed across silos |
| Edge Handling | Internal only | Cross-chunk communication |
| Performance | Simple, fast | Parallel processing |
| Complexity | Low | Higher (coordination) |
| Use Case | Demo, small grids | Production, large universes |

## Backwards Compatibility

The original single-chunk API is preserved:
- `GET /warmup` - Still works (uses single chunk)
- `GET /step` - Still works (single chunk)

New distributed API is separate:
- `POST /universe/init` - Multi-chunk initialization
- `POST /universe/step` - Multi-chunk advancement

## Summary

This implementation enables true distributed processing of Conway's Game of Life across multiple Orleans grains that can communicate with each other. The system is designed to scale horizontally and is ready for Redis persistence when needed for multi-silo deployment.

Key advantages:
? Parallel chunk processing
? Seamless cross-chunk communication
? Scalable to large universes
? Orleans-native patterns
? Ready for Redis migration
? Backwards compatible
