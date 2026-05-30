# Quick Start Guide: Distributed Chunks

## Testing the Distributed Multi-Chunk System

### Prerequisites
1. Run the application: `dotnet run --project Sinapsa.GoL.DistOrleansProc`
2. Ensure Orleans Dashboard is accessible: http://localhost:8000/dashboard
3. Ensure API is accessible: http://localhost:5050

---

## API Endpoints

### 1. Initialize Distributed Universe

**Endpoint**: `POST /api/init`

**Parameters**:
- `chunksX` (default: 2) - Number of chunks horizontally
- `chunksY` (default: 2) - Number of chunks vertically  
- `chunkWidth` (default: 32) - Width of each chunk in cells
- `chunkHeight` (default: 32) - Height of each chunk in cells
- `liveDensity` (default: 0.15) - Initial percentage of live cells (0.0-1.0)

**Example Requests**:

```bash
# Small 2x2 grid (total 64x64 cells)
curl -X POST "http://localhost:5050/api/init?chunksX=2&chunksY=2&chunkWidth=32&chunkHeight=32&liveDensity=0.15"

# Medium 3x3 grid (total 96x96 cells)
curl -X POST "http://localhost:5050/api/init?chunksX=3&chunksY=3&chunkWidth=32&chunkHeight=32&liveDensity=0.2"

# Large 4x4 grid (total 128x128 cells)
curl -X POST "http://localhost:5050/api/init?chunksX=4&chunksY=4&chunkWidth=32&chunkHeight=32&liveDensity=0.1"
```

**Response**:
```json
{
  "message": "Distributed universe initialized",
  "configuration": {
    "chunksX": 2,
    "chunksY": 2,
    "chunkWidth": 32,
    "chunkHeight": 32,
    "totalWidth": 64,
    "totalHeight": 64,
    "totalChunks": 4,
    "liveDensity": 0.15
  }
}
```

### 2. Advance Universe by One Generation

**Endpoint**: `POST /api/step`

**Example**:
```bash
curl -X POST http://localhost:5050/api/step
```

**Response**: ASCII visualization of the entire universe

### 3. View Current State

**Endpoint**: `GET /api/state`

**Example**:
```bash
curl http://localhost:5050/api/state
```

**Response**: ASCII visualization of the entire universe

---

## Testing Scenarios

### Scenario 1: Basic Distributed Setup

```bash
# 1. Initialize 2x2 chunks
curl -X POST "http://localhost:5050/api/init?chunksX=2&chunksY=2"

# 2. Run 5 generations
for i in {1..5}; do
  echo "Generation $i:"
  curl -X POST http://localhost:5050/api/step
  echo ""
done

# 3. Check Orleans Dashboard
# Navigate to: http://localhost:8000/dashboard
# You should see 4 grains: chunk_0_0, chunk_0_1, chunk_1_0, chunk_1_1
```

### Scenario 2: Large Universe

```bash
# Initialize a large universe (200x200 cells across 16 chunks)
curl -X POST "http://localhost:5050/api/init?chunksX=4&chunksY=4&chunkWidth=50&chunkHeight=50&liveDensity=0.1"

# Run simulation
curl -X POST http://localhost:5050/api/step

# Check dashboard for 16 grain activations
```

### Scenario 3: Sparse Population

```bash
# Create a sparse universe (5% alive)
curl -X POST "http://localhost:5050/api/init?chunksX=3&chunksY=3&chunkWidth=40&chunkHeight=40&liveDensity=0.05"

# Observe how patterns evolve
for i in {1..10}; do
  curl -X POST http://localhost:5050/api/step > "generation_$i.txt"
done
```

---

## Verifying Inter-Chunk Communication

### Check in Orleans Dashboard

1. Navigate to http://localhost:8000/dashboard
2. Click on "Grains" tab
3. Look for grains named `chunk_X_Y`
4. Click on a grain to see method invocations
5. Verify you see calls to:
   - `Advance()`
   - `GetTopEdge()`, `GetBottomEdge()`, etc.
   - `SetNeighborChunks()`

### Expected Grain Call Pattern

For a 2x2 grid, you should see:

**chunk_0_0** (top-left):
- Calls `GetLeftEdge()` on **chunk_1_0** (right neighbor)
- Calls `GetTopEdge()` on **chunk_0_1** (bottom neighbor)
- Calls `GetTopLeftCorner()` on **chunk_1_1** (diagonal neighbor)

**chunk_1_0** (top-right):
- Calls `GetRightEdge()` on **chunk_0_0** (left neighbor)
- Calls `GetTopEdge()` on **chunk_1_1** (bottom neighbor)
- Calls `GetTopRightCorner()` on **chunk_0_1** (diagonal neighbor)

**chunk_0_1** (bottom-left):
- Calls `GetLeftEdge()` on **chunk_1_1** (right neighbor)
- Calls `GetBottomEdge()` on **chunk_0_0** (top neighbor)
- Calls `GetBottomLeftCorner()` on **chunk_1_0** (diagonal neighbor)

**chunk_1_1** (bottom-right):
- Calls `GetRightEdge()` on **chunk_0_1** (left neighbor)
- Calls `GetBottomEdge()` on **chunk_1_0** (top neighbor)
- Calls `GetBottomRightCorner()` on **chunk_0_0** (diagonal neighbor)

---

## Debugging Tips

### View Detailed Logs

Add to `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Orleans": "Information",
      "Sinapsa.GoL.DistOrleansProc": "Debug"
    }
  }
}
```

### Common Issues

**Issue**: "Chunks not communicating"
- **Check**: Verify `SetNeighborChunks()` was called during init
- **Solution**: Reinitialize the universe

**Issue**: "Grains not showing in dashboard"
- **Check**: Dashboard configuration in `appsettings.json`
- **Solution**: Ensure `UseDashboard: true` and dashboard port is 8000

**Issue**: "Edge cells behaving incorrectly"
- **Check**: Neighbor IDs in grain state
- **Debug**: Add logging in `FetchNeighborEdges()` method

---

## Performance Testing

### Measure Throughput

```bash
# Run 100 generations and measure time
time for i in {1..100}; do
  curl -s -X POST http://localhost:5050/api/step > /dev/null
done
```

### Monitor Resource Usage

```bash
# Watch dashboard metrics while running:
curl -X POST "http://localhost:5050/api/init?chunksX=5&chunksY=5&chunkWidth=40&chunkHeight=40"

# In another terminal, run continuous steps:
while true; do
  curl -s -X POST http://localhost:5050/api/step > /dev/null
  sleep 0.1
done
```

---

## Comparing Single vs Multi-Chunk

### Single Chunk (Original API)

```bash
# Initialize
curl http://localhost:5050/warmup

# Step
curl http://localhost:5050/step
```

### Multi-Chunk (New API)

```bash
# Initialize
curl -X POST "http://localhost:5050/universe/init?chunksX=1&chunksY=1&chunkWidth=32&chunkHeight=32"

# Step
curl -X POST http://localhost:5050/universe/step
```

**Note**: Even a 1x1 chunk grid uses the new distributed infrastructure!

---

## Swagger UI

For interactive API testing:

1. Navigate to: http://localhost:5050/swagger
2. Expand "api" endpoints
3. Try the endpoints with different parameters
4. View request/response schemas

---

## Next Steps: Redis Migration

When ready to migrate to Redis for multi-silo deployment:

1. Add Redis NuGet package:
   ```bash
   dotnet add package Microsoft.Orleans.Persistence.Redis
   ```

2. Update `Program.cs`:
   ```csharp
   siloBuilder.AddRedisGrainStorage("RedisChunkStorage", options =>
   {
       options.ConnectionString = "localhost:6379";
   });
   ```

3. Update `GoLChunkGrain.cs`:
   ```csharp
   [StorageProvider(ProviderName = "RedisChunkStorage")]
   ```

4. Test with multiple silo instances!

---

## Useful Commands

```bash
# Clean state (restart application)
# CTRL+C to stop, then restart

# View all active grains via Orleans Dashboard
open http://localhost:8000/dashboard

# View Swagger documentation
open http://localhost:5050/swagger

# Health check
curl http://localhost:5050/hc
```

## Support

For issues or questions:
- Check the documentation: `DISTRIBUTED_CHUNKS.md`
- Review architecture: `ARCHITECTURE.md`
- Check Orleans logs in console output
- Monitor dashboard: http://localhost:8000/dashboard
