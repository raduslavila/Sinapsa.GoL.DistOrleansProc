using Microsoft.AspNetCore.Mvc;
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

        // Configuration for distributed universe
        private int _chunksX;
        private int _chunksY;
        private int _chunkSize;

        public GoLService(IGrainFactory<IGoLChunkGrain, string> grainChunkFactory)
        {
            _grainChunkFactory = grainChunkFactory;
        }

        public async Task InitUniverse(int chunksX, int chunksY, int chunkSize, double liveDensity)
        {
            _chunksX = chunksX;
            _chunksY = chunksY;
            _chunkSize = chunkSize;

            // Initialize all chunks
            var initTasks = new List<Task>();

            for (int x = 0; x < chunksX; x++)
            {
                for (int y = 0; y < chunksY; y++)
                {
                    var chunkId = GetChunkId(x, y);
                    var chunk = _grainChunkFactory.GetGrain(chunkId);
                    initTasks.Add(chunk.InitChunk(x, y, chunkSize, liveDensity));
                }
            }

            await Task.WhenAll(initTasks);

            // Set up neighbor relationships
            await ConfigureChunkNeighbors();
        }

        public async Task RunUniverseStep()
        {
            var advanceTasks = new List<Task>();

            // Advance all chunks in parallel
            for (int x = 0; x < _chunksX; x++)
            {
                for (int y = 0; y < _chunksY; y++)
                {
                    var chunkId = GetChunkId(x, y);
                    var chunk = _grainChunkFactory.GetGrain(chunkId);
                    advanceTasks.Add(chunk.Advance());
                }
            }

            // TODO: When using Redis, consider implementing optimistic concurrency control
            // to handle concurrent updates across distributed silos
            await Task.WhenAll(advanceTasks);
        }

        public async Task<string> DisplayUniverseState()
        {
            var sb = new StringBuilder();

            // Fetch all chunk states
            var chunkStates = new Cell[_chunksX, _chunksY][,];

            for (int cx = 0; cx < _chunksX; cx++)
            {
                for (int cy = 0; cy < _chunksY; cy++)
                {
                    var chunkId = GetChunkId(cx, cy);
                    var chunk = _grainChunkFactory.GetGrain(chunkId);
                    chunkStates[cx, cy] = await chunk.GetChunk();
                }
            }

            // Build the complete universe display
            for (int cy = 0; cy < _chunksY; cy++)
            {
                for (int y = 0; y < _chunkSize; y++)
                {
                    for (int cx = 0; cx < _chunksX; cx++)
                    {
                        for (int x = 0; x < _chunkSize; x++)
                        {
                            sb.Append(chunkStates[cx, cy][x, y].IsAlive ? "#" : " ");
                        }
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
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
