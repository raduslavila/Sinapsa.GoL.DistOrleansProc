using Orleans;
using Orleans.Providers;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models;

namespace Sinapsa.GoL.DistOrleansProc.Grains
{
    /// <summary>
    /// Singleton universe coordinator grain (primary key = "universe").
    /// Because Orleans guarantees a single activation per grain key across the whole cluster,
    /// this grain is the one authoritative source of truth for universe dimensions,
    /// generation counter, and grid snapshots — fixing the multi-silo inconsistency that
    /// existed when GoLService (a per-silo DI singleton) held that state.
    /// </summary>
    [StorageProvider(ProviderName = "GoLUniverseStore")]
    public class GoLUniverseGrain : Grain<GoLUniverseGrainState>, IGoLUniverseGrain
    {
        // Grid snapshots are large; we keep them in grain-activation memory only.
        // If the grain is deactivated and reactivated the snapshots are rebuilt from chunk grains.
        // (State.CurrentGeneration and dimensions survive via the storage provider.)
        private bool[][]? _currentGrid;
        private bool[][]? _previousGrid;

        // ── Public API ────────────────────────────────────────────────────────────

        public Task<bool> IsInitialized()
        {
            return Task.FromResult(State.IsInitialized);
        }

        public async Task InitUniverse(int chunksX, int chunksY, int chunkSize, double liveDensity)
        {
            await InitInternal(chunksX, chunksY, chunkSize, liveDensity);
        }

        public async Task ClearAndReinitUniverse(int chunksX, int chunksY, int chunkSize, double liveDensity)
        {
            var configChanged = !State.IsInitialized
                                || State.ChunksX != chunksX
                                || State.ChunksY != chunksY
                                || State.ChunkSize != chunkSize;

            if (configChanged)
            {
                await InitInternal(chunksX, chunksY, chunkSize, liveDensity);
                return;
            }

            // Same config → just randomise cells, keep topology
            var clearTasks = ChunkCoords().Select(c =>
                GetChunkGrain(c.x, c.y).Clear()).ToList();
            await Task.WhenAll(clearTasks);

            var reinitTasks = ChunkCoords().Select(c =>
                GetChunkGrain(c.x, c.y).InitChunk(c.x, c.y, State.ChunkSize, liveDensity)).ToList();
            await Task.WhenAll(reinitTasks);

            _previousGrid = null;
            _currentGrid = await BuildGrid();
            State.CurrentGeneration = 0;
            await WriteStateAsync();
        }

        public async Task StepUniverse()
        {
            // Advance all chunks in parallel
            var advanceTasks = ChunkCoords().Select(c =>
                GetChunkGrain(c.x, c.y).Advance()).ToList();
            await Task.WhenAll(advanceTasks);

            _previousGrid = _currentGrid;
            _currentGrid = await BuildGrid();
            State.CurrentGeneration++;
            await WriteStateAsync();
        }

        public async Task<UniverseStateDto> GetState()
        {
            if (_currentGrid == null)
                _currentGrid = await BuildGrid();

            return ToStateDto(_currentGrid);
        }

        public async Task<UniverseUpdateDto> GetUpdate(int lastSeenGeneration)
        {
            if (_currentGrid == null)
                _currentGrid = await BuildGrid();

            var current    = _currentGrid;
            var previous   = _previousGrid;
            var generation = State.CurrentGeneration;
            var width      = State.ChunksX * State.ChunkSize;
            var height     = State.ChunksY * State.ChunkSize;

            var requiresFullGrid = previous == null
                                   || lastSeenGeneration < 0
                                   || lastSeenGeneration > generation
                                   || generation - lastSeenGeneration > 1;

            if (requiresFullGrid)
            {
                return new UniverseUpdateDto
                {
                    CurrentGeneration = generation,
                    IsFullGrid        = true,
                    Grid              = ToStateDto(current)
                };
            }

            if (lastSeenGeneration == generation)
            {
                return new UniverseUpdateDto
                {
                    CurrentGeneration = generation,
                    IsFullGrid        = false,
                    Deltas            = new List<UniverseCellDeltaDto>()
                };
            }

            var deltas = new List<UniverseCellDeltaDto>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (previous![x][y] != current[x][y])
                        deltas.Add(new UniverseCellDeltaDto { X = x, Y = y, IsAlive = current[x][y] });
                }
            }

            return new UniverseUpdateDto
            {
                CurrentGeneration = generation,
                IsFullGrid        = false,
                Deltas            = deltas
            };
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private async Task InitInternal(int chunksX, int chunksY, int chunkSize, double liveDensity)
        {
            State.ChunksX      = chunksX;
            State.ChunksY      = chunksY;
            State.ChunkSize    = chunkSize;
            State.IsInitialized = true;

            var initTasks = ChunkCoords().Select(c =>
                GetChunkGrain(c.x, c.y).InitChunk(c.x, c.y, chunkSize, liveDensity)).ToList();
            await Task.WhenAll(initTasks);

            await ConfigureNeighbors();

            _previousGrid = null;
            _currentGrid  = await BuildGrid();
            State.CurrentGeneration = 0;
            await WriteStateAsync();
        }

        private async Task ConfigureNeighbors()
        {
            var tasks = ChunkCoords().Select(c =>
            {
                var (x, y) = c;
                return GetChunkGrain(x, y).SetNeighborChunks(
                    y > 0                              ? ChunkId(x, y - 1) : null!,
                    y < State.ChunksY - 1             ? ChunkId(x, y + 1) : null!,
                    x > 0                              ? ChunkId(x - 1, y) : null!,
                    x < State.ChunksX - 1             ? ChunkId(x + 1, y) : null!,
                    (x > 0 && y > 0)                  ? ChunkId(x - 1, y - 1) : null!,
                    (x < State.ChunksX-1 && y > 0)   ? ChunkId(x + 1, y - 1) : null!,
                    (x > 0 && y < State.ChunksY-1)   ? ChunkId(x - 1, y + 1) : null!,
                    (x < State.ChunksX-1 && y < State.ChunksY-1) ? ChunkId(x + 1, y + 1) : null!
                );
            }).ToList();
            await Task.WhenAll(tasks);
        }

        private async Task<bool[][]> BuildGrid()
        {
            var cx   = State.ChunksX;
            var cy   = State.ChunksY;
            var size = State.ChunkSize;
            var w    = cx * size;
            var h    = cy * size;

            // Fetch all chunks in parallel
            var chunkData = new Cell[cx, cy][,];
            var fetchTasks = ChunkCoords().Select(async c =>
            {
                chunkData[c.x, c.y] = await GetChunkGrain(c.x, c.y).GetChunk();
            }).ToList();
            await Task.WhenAll(fetchTasks);

            var cells = new bool[w][];
            for (int x = 0; x < w; x++)
                cells[x] = new bool[h];

            for (int chunkY = 0; chunkY < cy; chunkY++)
            for (int localY = 0; localY < size; localY++)
            for (int chunkX = 0; chunkX < cx; chunkX++)
            for (int localX = 0; localX < size; localX++)
            {
                cells[chunkX * size + localX][chunkY * size + localY] =
                    chunkData[chunkX, chunkY][localX, localY].IsAlive;
            }

            return cells;
        }

        private UniverseStateDto ToStateDto(bool[][] grid)
        {
            var w = State.ChunksX * State.ChunkSize;
            var h = State.ChunksY * State.ChunkSize;
            var snapshot = new bool[w][];
            for (int x = 0; x < w; x++)
            {
                snapshot[x] = new bool[h];
                Array.Copy(grid[x], snapshot[x], h);
            }
            return new UniverseStateDto
            {
                Width      = w,
                Height     = h,
                Generation = State.CurrentGeneration,
                Cells      = snapshot
            };
        }

        private IEnumerable<(int x, int y)> ChunkCoords()
        {
            for (int x = 0; x < State.ChunksX; x++)
            for (int y = 0; y < State.ChunksY; y++)
                yield return (x, y);
        }

        private IGoLChunkGrain GetChunkGrain(int x, int y) =>
            GrainFactory.GetGrain<IGoLChunkGrain>(ChunkId(x, y));

        private static string ChunkId(int x, int y) => $"chunk_{x}_{y}";
    }
}
