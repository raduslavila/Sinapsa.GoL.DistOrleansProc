# Refactoring: Square Chunks Only

## Overview

Refactored the codebase to enforce that all chunks are **square** (Size x Size) instead of allowing rectangular chunks with separate width and height dimensions.

---

## Changes Made

### 1. **GoLChunkGrainState** - State Model

**Before:**
```csharp
public int Width { get; set; }
public int Height { get; set; }
public int ChunkLocationy { get; set; }  // Note: typo
```

**After:**
```csharp
public int Size { get; set; }  // Chunks are always square (Size x Size)
public int ChunkLocationY { get; set; }  // Fixed typo
```

**Benefits:**
- ? Simpler state model
- ? Fixed typo in `ChunkLocationy` ? `ChunkLocationY`
- ? Single source of truth for chunk dimensions

---

### 2. **IGoLChunkGrain** - Grain Interface

**Before:**
```csharp
Task InitChunk(int chunkX, int chunkY, int width, int height, double liveDensity);
```

**After:**
```csharp
Task InitChunk(int chunkX, int chunkY, int size, double liveDensity);
```

**Benefits:**
- ? Simpler method signature
- ? Impossible to create non-square chunks
- ? Clearer intent

---

### 3. **GoLChunkGrain** - Implementation

#### ConnectNeighbors Method

**Before:**
```csharp
for (int x = 0; x < currentState.Width; x++)
{
    for (int y = 0; y < currentState.Height; y++)
    {
        bool isRightEdge = (x == currentState.Width - 1);
        bool isBottomEdge = (y == currentState.Height - 1);
        // ...
    }
}
```

**After:**
```csharp
for (int x = 0; x < currentState.Size; x++)
{
    for (int y = 0; y < currentState.Size; y++)
    {
        bool isRightEdge = (x == currentState.Size - 1);
        bool isBottomEdge = (y == currentState.Size - 1);
        // ...
    }
}
```

#### InitChunk Method

**Before:**
```csharp
public async Task InitChunk(int chunkX, int chunkY, int width, int height, double liveDensity)
{
    this.State = new GoLChunkGrainState
    {
        Width = width,
        Height = height,
        Cells = new Cell[width, height]
    };

    for (int x = 0; x < this.State.Width; x++)
        for (int y = 0; y < this.State.Height; y++)
            this.State.Cells[x, y] = new Cell();
}
```

**After:**
```csharp
public async Task InitChunk(int chunkX, int chunkY, int size, double liveDensity)
{
    this.State = new GoLChunkGrainState
    {
        Size = size,
        Cells = new Cell[size, size]  // Always square!
    };

    for (int x = 0; x < this.State.Size; x++)
        for (int y = 0; y < this.State.Size; y++)
            this.State.Cells[x, y] = new Cell();
}
```

#### Advance Method

All references to `Width` and `Height` replaced with `Size`:
```csharp
for (int w = 0; w < currentState.Size; w++)
{
    for (int h = 0; h < currentState.Size; h++)
    {
        // Process cell...
    }
}
```

#### Edge Methods

**Before:**
```csharp
var edge = new bool[State.Width];  // or State.Height
for (int x = 0; x < State.Width; x++)
```

**After:**
```csharp
var edge = new bool[State.Size];
for (int x = 0; x < State.Size; x++)
```

---

### 4. **IGoLService** - Service Interface

**Before:**
```csharp
Task InitUniverse(int chunksX, int chunksY, int chunkWidth, int chunkHeight, double liveDensity);
```

**After:**
```csharp
Task InitUniverse(int chunksX, int chunksY, int chunkSize, double liveDensity);
```

---

### 5. **GoLService** - Service Implementation

#### Fields

**Before:**
```csharp
private int _chunkWidth;
private int _chunkHeight;
```

**After:**
```csharp
private int _chunkSize;
```

#### InitUniverse Method

**Before:**
```csharp
public async Task InitUniverse(int chunksX, int chunksY, int chunkWidth, int chunkHeight, double liveDensity)
{
    _chunkWidth = chunkWidth;
    _chunkHeight = chunkHeight;

    initTasks.Add(chunk.InitChunk(x, y, chunkWidth, chunkHeight, liveDensity));
}
```

**After:**
```csharp
public async Task InitUniverse(int chunksX, int chunksY, int chunkSize, double liveDensity)
{
    _chunkSize = chunkSize;

    initTasks.Add(chunk.InitChunk(x, y, chunkSize, liveDensity));
}
```

#### DisplayUniverseState Method

**Before:**
```csharp
for (int y = 0; y < _chunkHeight; y++)
{
    for (int x = 0; x < _chunkWidth; x++)
    {
        // ...
    }
}
```

**After:**
```csharp
for (int y = 0; y < _chunkSize; y++)
{
    for (int x = 0; x < _chunkSize; x++)
    {
        // ...
    }
}
```

---

### 6. **UniverseController** - API Controller

#### InitUniverse Endpoint

**Before:**
```csharp
[HttpPost("init")]
public async Task<IActionResult> InitUniverse(
    [FromQuery] int chunksX = 2,
    [FromQuery] int chunksY = 2,
    [FromQuery] int chunkWidth = 32,
    [FromQuery] int chunkHeight = 32,
    [FromQuery] double liveDensity = 0.15)
{
    await _goLService.InitUniverse(chunksX, chunksY, chunkWidth, chunkHeight, liveDensity);

    return Ok(new
    {
        chunkWidth,
        chunkHeight,
        totalWidth = chunksX * chunkWidth,
        totalHeight = chunksY * chunkHeight,
        totalChunks = chunksX * chunksY
    });
}
```

**After:**
```csharp
[HttpPost("init")]
public async Task<IActionResult> InitUniverse(
    [FromQuery] int chunksX = 2,
    [FromQuery] int chunksY = 2,
    [FromQuery] int chunkSize = 32,
    [FromQuery] double liveDensity = 0.15)
{
    await _goLService.InitUniverse(chunksX, chunksY, chunkSize, liveDensity);

    return Ok(new
    {
        chunkSize,
        totalWidth = chunksX * chunkSize,
        totalHeight = chunksY * chunkSize,
        totalCells = chunksX * chunkSize * chunksY * chunkSize,  // New!
        totalChunks = chunksX * chunksY
    });
}
```

---

## API Changes

### Before

```bash
curl -X POST "http://localhost:5050/api/init?chunksX=2&chunksY=2&chunkWidth=32&chunkHeight=32"
```

**Response:**
```json
{
  "configuration": {
    "chunksX": 2,
    "chunksY": 2,
    "chunkWidth": 32,
    "chunkHeight": 32,
    "totalWidth": 64,
    "totalHeight": 64,
    "totalChunks": 4
  }
}
```

### After

```bash
curl -X POST "http://localhost:5050/api/init?chunksX=2&chunksY=2&chunkSize=32"
```

**Response:**
```json
{
  "configuration": {
    "chunksX": 2,
    "chunksY": 2,
    "chunkSize": 32,
    "totalWidth": 64,
    "totalHeight": 64,
    "totalCells": 4096,
    "totalChunks": 4
  }
}
```

---

## Benefits

### ? Simplification
- **Fewer parameters**: 4 params ? 3 params for initialization
- **Fewer fields**: 2 dimension fields ? 1 dimension field
- **Clearer code**: No confusion about width vs height

### ? Consistency
- All chunks are guaranteed to be square
- Easier to reason about chunk dimensions
- Uniform chunk processing

### ? Performance
- Same performance characteristics
- Slightly less memory (1 int instead of 2)
- Cleaner code paths

### ? Bug Prevention
- Impossible to accidentally create 32x64 chunk when you wanted 32x32
- No width/height swapping bugs
- Simpler validation

### ? Better Semantics
- "Chunk size" is more intuitive than "chunk width and height"
- Square chunks are natural for Game of Life grids
- Matches typical grid partitioning patterns

---

## Migration Impact

### Breaking Changes

**API Parameter Changes:**
- ? `chunkWidth` parameter removed
- ? `chunkHeight` parameter removed
- ? `chunkSize` parameter added

**Service Interface Changes:**
- ? `InitUniverse(chunksX, chunksY, chunkWidth, chunkHeight, liveDensity)` removed
- ? `InitUniverse(chunksX, chunksY, chunkSize, liveDensity)` added

**Grain Interface Changes:**
- ? `InitChunk(chunkX, chunkY, width, height, liveDensity)` removed
- ? `InitChunk(chunkX, chunkY, size, liveDensity)` added

### Migration Examples

**Old Code:**
```csharp
await service.InitUniverse(chunksX: 4, chunksY: 4, chunkWidth: 32, chunkHeight: 32, liveDensity: 0.15);
```

**New Code:**
```csharp
await service.InitUniverse(chunksX: 4, chunksY: 4, chunkSize: 32, liveDensity: 0.15);
```

**Old API Call:**
```bash
curl -X POST "http://localhost:5050/api/init?chunksX=3&chunksY=3&chunkWidth=50&chunkHeight=50&liveDensity=0.2"
```

**New API Call:**
```bash
curl -X POST "http://localhost:5050/api/init?chunksX=3&chunksY=3&chunkSize=50&liveDensity=0.2"
```

---

## Examples

### Initialize 2x2 Grid with 32x32 Chunks

```bash
curl -X POST "http://localhost:5050/api/init?chunksX=2&chunksY=2&chunkSize=32"
```

**Total Universe**: 64x64 cells (4,096 cells)

### Initialize 4x4 Grid with 50x50 Chunks

```bash
curl -X POST "http://localhost:5050/api/init?chunksX=4&chunksY=4&chunkSize=50"
```

**Total Universe**: 200x200 cells (40,000 cells)

### Initialize Large Grid

```bash
curl -X POST "http://localhost:5050/api/init?chunksX=10&chunksY=10&chunkSize=100"
```

**Total Universe**: 1000x1000 cells (1,000,000 cells!)

---

## Additional Fixes

### Fixed Typo in GoLChunkGrainState

**Before:**
```csharp
public int ChunkLocationy { get; set; }  // lowercase 'y'
```

**After:**
```csharp
public int ChunkLocationY { get; set; }  // uppercase 'Y'
```

---

## Code Statistics

### Lines Changed
- **GoLChunkGrainState.cs**: 3 lines modified
- **IGoLChunkGrain.cs**: 1 line modified
- **GoLChunkGrain.cs**: ~40 occurrences of Width/Height ? Size
- **IGoLService.cs**: 1 line modified
- **GoLService.cs**: ~15 lines modified
- **UniverseController.cs**: ~15 lines modified

### Total Impact
- **6 files** modified
- **~75 lines** changed
- **0 files** deleted
- **0 files** added

---

## Build Status

? **Build Successful**
```
Build succeeded.
    0 Error(s)
```

---

## Testing

### Quick Test

```bash
# Initialize square chunks
curl -X POST "http://localhost:5050/api/init?chunksX=2&chunksY=2&chunkSize=32&liveDensity=0.15"

# Run simulation
curl -X POST http://localhost:5050/api/step

# View state
curl http://localhost:5050/api/state

# Check Orleans Dashboard
# http://localhost:8000/dashboard
# Should see 4 square chunks (32x32 each)
```

---

## Summary

The refactoring successfully enforces **square chunks only**, simplifying the codebase and preventing potential bugs from width/height mismatches. All chunks are now guaranteed to be Size x Size dimensions, with cleaner APIs and clearer semantics.

**Key Achievement**: Reduced complexity while maintaining full functionality! ??
