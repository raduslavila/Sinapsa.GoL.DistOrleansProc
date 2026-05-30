# Changed Cells Tracking

## Overview

The `Advance()` method in `GoLChunkGrain` now captures and tracks which cells changed between generations using a lambda expression.

## Implementation

Before updating cells to their next state, the code captures all changed cells:

```csharp
// Capture changed cells (diff between IsAliveNext and IsAlive)
var changedCells = Enumerable.Range(0, currentState.Width)
    .SelectMany(w => Enumerable.Range(0, currentState.Height)
        .Where(h => currentState.Cells[w, h].IsAlive != currentState.Cells[w, h].IsAliveNext)
        .Select(h => new
        {
            X = w,
            Y = h,
            WasAlive = currentState.Cells[w, h].IsAlive,
            WillBeAlive = currentState.Cells[w, h].IsAliveNext,
            Change = currentState.Cells[w, h].IsAliveNext ? "Born" : "Died"
        }))
    .ToList();
```

## Data Captured

Each changed cell includes:

- **X**: X coordinate in the chunk
- **Y**: Y coordinate in the chunk
- **WasAlive**: Previous state (before this generation)
- **WillBeAlive**: New state (after this generation)
- **Change**: Human-readable description ("Born" or "Died")

## Usage Examples

### 1. Log Changed Cells

```csharp
// After the changedCells declaration, add:
foreach (var cell in changedCells)
{
    Console.WriteLine($"Chunk {State.ChunkId}: Cell ({cell.X}, {cell.Y}) {cell.Change}");
}
```

### 2. Count Changes

```csharp
var birthCount = changedCells.Count(c => c.Change == "Born");
var deathCount = changedCells.Count(c => c.Change == "Died");
Console.WriteLine($"Chunk {State.ChunkId}: {birthCount} births, {deathCount} deaths");
```

### 3. Return Change Statistics

You could modify the `Advance()` method to return statistics:

```csharp
public async Task<ChangeStatistics> Advance()
{
    // ... existing code ...

    var changedCells = /* ... lambda expression ... */;

    // Update cells
    // ...

    return new ChangeStatistics
    {
        ChunkId = State.ChunkId,
        TotalChanges = changedCells.Count,
        Births = changedCells.Count(c => c.Change == "Born"),
        Deaths = changedCells.Count(c => c.Change == "Died"),
        ChangedCells = changedCells.Select(c => (c.X, c.Y)).ToList()
    };
}
```

### 4. Track Hot Spots

Identify areas with frequent changes:

```csharp
var hotSpots = changedCells
    .GroupBy(c => (c.X / 10, c.Y / 10)) // Group into 10x10 regions
    .Select(g => new
    {
        Region = g.Key,
        ChangeCount = g.Count()
    })
    .OrderByDescending(r => r.ChangeCount)
    .ToList();
```

## Performance Considerations

### Current Implementation
- The lambda expression is evaluated once per `Advance()` call
- Uses LINQ for clean, functional code
- Creates a list only of changed cells (not all cells)

### Memory Usage
- Only cells that changed are stored in the list
- For sparse grids with few changes, very memory efficient
- For dense grids with many changes, list will be larger

### Optimization Options

If you need better performance, consider:

1. **Use a counter instead of list**:
```csharp
var changeCount = Enumerable.Range(0, currentState.Width)
    .Sum(w => Enumerable.Range(0, currentState.Height)
        .Count(h => currentState.Cells[w, h].IsAlive != currentState.Cells[w, h].IsAliveNext));
```

2. **Filter for specific types of changes**:
```csharp
// Only track births
var births = Enumerable.Range(0, currentState.Width)
    .SelectMany(w => Enumerable.Range(0, currentState.Height)
        .Where(h => !currentState.Cells[w, h].IsAlive && currentState.Cells[w, h].IsAliveNext)
        .Select(h => (X: w, Y: h)))
    .ToList();
```

3. **Add to interface for external consumption**:
```csharp
// In IGoLChunkGrain
Task<(int births, int deaths)> GetLastChangeStatistics();

// In GoLChunkGrain
private int _lastBirths;
private int _lastDeaths;

public Task<(int births, int deaths)> GetLastChangeStatistics()
{
    return Task.FromResult((_lastBirths, _lastDeaths));
}
```

## Integration with Logging

To log changes to Orleans logging infrastructure:

```csharp
if (changedCells.Any())
{
    var logger = GetLogger();
    logger.LogInformation(
        "Chunk {ChunkId} at ({ChunkX}, {ChunkY}): {Changes} cells changed ({Births} born, {Deaths} died)",
        State.ChunkId,
        State.ChunkLocationX,
        State.ChunkLocationy,
        changedCells.Count,
        changedCells.Count(c => c.Change == "Born"),
        changedCells.Count(c => c.Change == "Died")
    );
}
```

## Debugging Specific Patterns

Track known Game of Life patterns:

```csharp
// Detect blinkers (cells that oscillate)
var potentialBlinkers = changedCells
    .Where(c => c.WasAlive != c.WillBeAlive)
    .Select(c => (c.X, c.Y))
    .ToHashSet();
```

## Visualization Support

Export changed cells for visualization:

```csharp
var changeMap = new bool[currentState.Width, currentState.Height];
foreach (var cell in changedCells)
{
    changeMap[cell.X, cell.Y] = true;
}
// Send changeMap to visualization client
```

## Example: Complete Usage in Advance()

```csharp
public async Task Advance()
{
    await ReadStateAsync();
    var currentState = this.State;
    var neighborEdges = await FetchNeighborEdges();

    // ... apply rules ...

    // Capture changes
    var changedCells = Enumerable.Range(0, currentState.Width)
        .SelectMany(w => Enumerable.Range(0, currentState.Height)
            .Where(h => currentState.Cells[w, h].IsAlive != currentState.Cells[w, h].IsAliveNext)
            .Select(h => new
            {
                X = w,
                Y = h,
                WasAlive = currentState.Cells[w, h].IsAlive,
                WillBeAlive = currentState.Cells[w, h].IsAliveNext,
                Change = currentState.Cells[w, h].IsAliveNext ? "Born" : "Died"
            }))
        .ToList();

    // Optional: Log or process changes
    if (changedCells.Any())
    {
        var logger = GetLogger();
        logger.LogDebug(
            "Chunk {ChunkId}: {ChangeCount} cells changed",
            State.ChunkId,
            changedCells.Count
        );
    }

    // Update all cells to their next state
    for (int w = 0; w < currentState.Width; w++)
    {
        for (int h = 0; h < currentState.Height; h++)
        {
            currentState.Cells[w, h].IsAlive = currentState.Cells[w, h].IsAliveNext;
        }
    }

    await WriteStateAsync();
}
```

## Future Enhancements

1. **Return change data from Advance()**:
   - Modify interface to return change statistics
   - Aggregate changes across all chunks in `GoLService`

2. **Add change history**:
   - Store last N generations of changes
   - Enable time-travel debugging

3. **Change-based optimizations**:
   - Only persist chunks with changes
   - Skip processing chunks with no active cells

4. **Pattern detection**:
   - Identify still lifes (no changes)
   - Detect oscillators (periodic changes)
   - Find gliders (moving patterns)

## Notes

- The `changedCells` variable is currently calculated but not used
- Add your own logic to log, return, or process these changes as needed
- The lambda expression is efficient - it only creates objects for cells that actually changed
- Perfect for debugging, monitoring, and understanding Game of Life dynamics
