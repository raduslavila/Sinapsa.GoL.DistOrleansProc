using Microsoft.AspNetCore.Mvc;
using Sinapsa.GoL.DistOrleansProc.Domain.Models;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models;
using Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sinapsa.GoL.DistOrleansProc.Domain.Services
{
    // TODO: When migrating to Redis, implement chunk state caching and distributed locking
    // to prevent race conditions across multiple silo instances
    public class GoLService : IGoLService
    {
        private readonly IGrainFactory<IGoLChunkGrain, string> _grainChunkFactory;
        private readonly IUniverseGridDeltaService _universeGridDeltaService;
        private readonly SemaphoreSlim _stateLock = new(1, 1);

        // Configuration for distributed universe
        private int _chunksX;
        private int _chunksY;
        private int _chunkSize;
        private int _currentGeneration;
        private GridStateDto? _currentGridSnapshot;
        private GridStateDto? _previousGridSnapshot;

        public GoLService(
            IGrainFactory<IGoLChunkGrain, string> grainChunkFactory,
            IUniverseGridDeltaService universeGridDeltaService)
        {
            _grainChunkFactory = grainChunkFactory;
            _universeGridDeltaService = universeGridDeltaService;
        }

        public async Task InitUniverse(int chunksX, int chunksY, int chunkSize, double liveDensity)
        {
            await _stateLock.WaitAsync();
            try
            {
                await InitUniverseInternal(chunksX, chunksY, chunkSize, liveDensity);
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public async Task ClearAndReinitUniverse(int chunksX, int chunksY, int chunkSize, double liveDensity)
        {
            await _stateLock.WaitAsync();
            try
            {
                var configChanged = _chunksX != chunksX || _chunksY != chunksY || _chunkSize != chunkSize;
                if (configChanged)
                {
                    await InitUniverseInternal(chunksX, chunksY, chunkSize, liveDensity);
                    return;
                }

                var clearTasks = new List<Task>();
                for (var x = 0; x < _chunksX; x++)
                {
                    for (var y = 0; y < _chunksY; y++)
                    {
                        var chunkId = GetChunkId(x, y);
                        var chunk = _grainChunkFactory.GetGrain(chunkId);
                        clearTasks.Add(chunk.Clear());
                    }
                }
                await Task.WhenAll(clearTasks);

                var reinitTasks = new List<Task>();
                for (var x = 0; x < _chunksX; x++)
                {
                    for (var y = 0; y < _chunksY; y++)
                    {
                        var chunkId = GetChunkId(x, y);
                        var chunk = _grainChunkFactory.GetGrain(chunkId);
                        reinitTasks.Add(chunk.InitChunk(x, y, chunkSize, liveDensity));
                    }
                }
                await Task.WhenAll(reinitTasks);

                _previousGridSnapshot = null;
                _currentGridSnapshot = await BuildUniverseGridFromGrainsInternal();
                _currentGeneration = 0;
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public async Task RunUniverseStep()
        {
            await _stateLock.WaitAsync();
            try
            {
                var advanceTasks = new List<Task>();

                // Advance all chunks in parallel
                for (var x = 0; x < _chunksX; x++)
                {
                    for (var y = 0; y < _chunksY; y++)
                    {
                        var chunkId = GetChunkId(x, y);
                        var chunk = _grainChunkFactory.GetGrain(chunkId);
                        advanceTasks.Add(chunk.Advance());
                    }
                }

                // TODO: When using Redis, consider implementing optimistic concurrency control
                // to handle concurrent updates across distributed silos
                await Task.WhenAll(advanceTasks);

                _previousGridSnapshot = _currentGridSnapshot;
                _currentGridSnapshot = await BuildUniverseGridFromGrainsInternal();
                _currentGeneration++;
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public async Task<string> DisplayUniverseState()
        {
            var universeGrid = await GetUniverseGrid();
            var sb = new StringBuilder();

            for (int y = 0; y < universeGrid.Height; y++)
            {
                for (int x = 0; x < universeGrid.Width; x++)
                {
                    sb.Append(universeGrid.Cells[x][y] ? "#" : " ");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public async Task<GridStateDto> GetUniverseGrid()
        {
            await _stateLock.WaitAsync();
            try
            {
                if (_currentGridSnapshot == null)
                {
                    _currentGridSnapshot = await BuildUniverseGridFromGrainsInternal();
                }

                return CloneGridState(_currentGridSnapshot);
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public async Task<UniverseGridUpdateDto> GetUniverseUpdate(int lastSeenGeneration)
        {
            await _stateLock.WaitAsync();
            try
            {
                if (_currentGridSnapshot == null)
                {
                    _currentGridSnapshot = await BuildUniverseGridFromGrainsInternal();
                }

                var current = CloneGridState(_currentGridSnapshot);
                var previous = _previousGridSnapshot != null ? CloneGridState(_previousGridSnapshot) : null;
                return _universeGridDeltaService.BuildUpdate(lastSeenGeneration, _currentGeneration, current, previous);
            }
            finally
            {
                _stateLock.Release();
            }
        }

        private async Task InitUniverseInternal(int chunksX, int chunksY, int chunkSize, double liveDensity)
        {
            _chunksX = chunksX;
            _chunksY = chunksY;
            _chunkSize = chunkSize;

            var initTasks = new List<Task>();
            for (var x = 0; x < chunksX; x++)
            {
                for (var y = 0; y < chunksY; y++)
                {
                    var chunkId = GetChunkId(x, y);
                    var chunk = _grainChunkFactory.GetGrain(chunkId);
                    initTasks.Add(chunk.InitChunk(x, y, chunkSize, liveDensity));
                }
            }

            await Task.WhenAll(initTasks);
            await ConfigureChunkNeighbors();

            _previousGridSnapshot = null;
            _currentGridSnapshot = await BuildUniverseGridFromGrainsInternal();
            _currentGeneration = 0;
        }

        private async Task<GridStateDto> BuildUniverseGridFromGrainsInternal()
        {
            var totalWidth = _chunksX * _chunkSize;
            var totalHeight = _chunksY * _chunkSize;

            var chunkStates = new Cell[_chunksX, _chunksY][,];
            for (var cx = 0; cx < _chunksX; cx++)
            {
                for (var cy = 0; cy < _chunksY; cy++)
                {
                    var chunkId = GetChunkId(cx, cy);
                    var chunk = _grainChunkFactory.GetGrain(chunkId);
                    chunkStates[cx, cy] = await chunk.GetChunk();
                }
            }

            var cells = new bool[totalWidth][];
            for (var x = 0; x < totalWidth; x++)
            {
                cells[x] = new bool[totalHeight];
            }

            for (var cy = 0; cy < _chunksY; cy++)
            {
                for (var y = 0; y < _chunkSize; y++)
                {
                    for (var cx = 0; cx < _chunksX; cx++)
                    {
                        for (var x = 0; x < _chunkSize; x++)
                        {
                            var globalX = cx * _chunkSize + x;
                            var globalY = cy * _chunkSize + y;
                            cells[globalX][globalY] = chunkStates[cx, cy][x, y].IsAlive;
                        }
                    }
                }
            }

            return new GridStateDto
            {
                Width = totalWidth,
                Height = totalHeight,
                Cells = cells
            };
        }

        private static GridStateDto CloneGridState(GridStateDto source)
        {
            var clonedCells = new bool[source.Width][];
            for (var x = 0; x < source.Width; x++)
            {
                clonedCells[x] = new bool[source.Height];
                Array.Copy(source.Cells[x], clonedCells[x], source.Height);
            }

            return new GridStateDto
            {
                Width = source.Width,
                Height = source.Height,
                Cells = clonedCells
            };
        }

        private async Task ConfigureChunkNeighbors()
        {
            var configTasks = new List<Task>();

            for (int x = 0; x < _chunksX; x++)
            {
                for (int y = 0; y < _chunksY; y++)
                {
                    var chunkId = GetChunkId(x, y);
                    var chunk = _grainChunkFactory.GetGrain(chunkId);

                    // Determine neighbor chunk IDs (null if out of bounds)
                    var topChunkId = y > 0 ? GetChunkId(x, y - 1) : null;
                    var bottomChunkId = y < _chunksY - 1 ? GetChunkId(x, y + 1) : null;
                    var leftChunkId = x > 0 ? GetChunkId(x - 1, y) : null;
                    var rightChunkId = x < _chunksX - 1 ? GetChunkId(x + 1, y) : null;

                    var topLeftChunkId = (x > 0 && y > 0) ? GetChunkId(x - 1, y - 1) : null;
                    var topRightChunkId = (x < _chunksX - 1 && y > 0) ? GetChunkId(x + 1, y - 1) : null;
                    var bottomLeftChunkId = (x > 0 && y < _chunksY - 1) ? GetChunkId(x - 1, y + 1) : null;
                    var bottomRightChunkId = (x < _chunksX - 1 && y < _chunksY - 1) ? GetChunkId(x + 1, y + 1) : null;

                    configTasks.Add(chunk.SetNeighborChunks(
                        topChunkId, bottomChunkId,
                        leftChunkId, rightChunkId,
                        topLeftChunkId, topRightChunkId,
                        bottomLeftChunkId, bottomRightChunkId));
                }
            }

            await Task.WhenAll(configTasks);
        }

        private string GetChunkId(int x, int y)
        {
            return $"chunk_{x}_{y}";
        }
    }
}
