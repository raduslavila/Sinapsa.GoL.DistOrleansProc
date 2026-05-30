using Orleans;
using Orleans;
using Orleans.Providers;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models;
using System.Diagnostics;

namespace Sinapsa.GoL.DistOrleansProc.Grains
{
    // TODO: Add Redis persistence support for distributed state management
    // Consider using [StorageProvider(ProviderName = "RedisGrainStorage")] when migrating to Redis
    [StorageProvider(ProviderName = "ChunkMemory")]
    public class GoLChunkGrain : Grain<GoLChunkGrainState>, IGoLChunkGrain
    {
        private Random rand = new Random();

        private void ConnectNeighbors()
        {
            var currentState = this.State;

            for (int x = 0; x < currentState.Width; x++)
            {
                rand = new Random();

                for (int y = 0; y < currentState.Height; y++)
                {
                    bool isLeftEdge = (x == 0);
                    bool isRightEdge = (x == currentState.Width - 1);
                    bool isTopEdge = (y == 0);
                    bool isBottomEdge = (y == currentState.Height - 1);
                    //bool isEdge = isLeftEdge | isRightEdge | isTopEdge | isBottomEdge;

                    //if (isEdge)
                    //    continue;

                    int xL = x - 1;
                    int xR = x + 1;
                    int yT = y - 1;
                    int yB = y + 1;

                    if (!isLeftEdge && !isTopEdge)
                    {
                        currentState.Cells[x, y].neighbors.Add(currentState.Cells[xL, yT]);
                    }
                    if (!isTopEdge)
                    {
                        currentState.Cells[x, y].neighbors.Add(currentState.Cells[x, yT]);
                    }
                    if (!isRightEdge && !isTopEdge)
                    {
                        currentState.Cells[x, y].neighbors.Add(currentState.Cells[xR, yT]);
                    }
                    if (!isLeftEdge)
                    {
                        currentState.Cells[x, y].neighbors.Add(currentState.Cells[xL, y]);
                    }
                    if (!isRightEdge)
                    {
                        currentState.Cells[x, y].neighbors.Add(currentState.Cells[xR, y]);
                    }
                    if (!isLeftEdge && !isBottomEdge)
                    {
                        currentState.Cells[x, y].neighbors.Add(currentState.Cells[xL, yB]);
                    }
                    if (!isBottomEdge)
                    {
                        currentState.Cells[x, y].neighbors.Add(currentState.Cells[x, yB]);
                    }
                    if (!isRightEdge && !isBottomEdge)
                    {
                        currentState.Cells[x, y].neighbors.Add(currentState.Cells[xR, yB]);
                    }
                }
            }
        }

        public async Task InitChunk(int chunkX, int chunkY, int width, int height, double liveDensity)
        {
            this.State = new GoLChunkGrainState
            {
                ChunkId = this.GetPrimaryKeyString(),
                ChunkLocationX = chunkX,
                ChunkLocationy = chunkY,
                Width = width,
                Height = height,
                Cells = new Cell[width, height]
            };

            for (int x = 0; x < this.State.Width; x++)
                for (int y = 0; y < this.State.Height; y++)
                    this.State.Cells[x, y] = new Cell();

            foreach (var cell in this.State.Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;

            ConnectNeighbors();

            await WriteStateAsync();
        }


        public async Task Advance()
        {
            await ReadStateAsync();

            // Fetch neighbor edge data for cross-chunk boundary cells
            var neighborEdges = await FetchNeighborEdges();

            for (int w = 0; w < State.Width; w++)
            {
                for (int h = 0; h < State.Height; h++)
                {
                    bool isLeftEdge = (w == 0);
                    bool isRightEdge = (w == State.Width - 1);
                    bool isTopEdge = (h == 0);
                    bool isBottomEdge = (h == State.Height - 1);

                    // Count neighbors within this chunk
                    int liveNeighbors = State.Cells[w, h].neighbors.Count(x => x.IsAlive);

                    // Add cross-chunk neighbors if on edge
                    liveNeighbors += CountCrossChunkNeighbors(w, h, isLeftEdge, isRightEdge, isTopEdge, isBottomEdge, neighborEdges);

                    // Apply Conway's Game of Life rules
                    if (State.Cells[w, h].IsAlive)
                        State.Cells[w, h].IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
                    else
                        State.Cells[w, h].IsAliveNext = liveNeighbors == 3;
                }
            }

            // Capture changed cells (diff between IsAliveNext and IsAlive)
            var changedCells = Enumerable.Range(0, State.Width)
                .SelectMany(w => Enumerable.Range(0, State.Height)
                    .Where(h => State.Cells[w, h].IsAlive != State.Cells[w, h].IsAliveNext)
                    .Select(h => new
                    {
                        X = w,
                        Y = h,
                        WasAlive = State.Cells[w, h].IsAlive,
                        WillBeAlive = State.Cells[w, h].IsAliveNext,
                        Change = State.Cells[w, h].IsAliveNext ? "Born" : "Died"
                    }))
                .ToList();

            // Update all cells to their next state
            for (int w = 0; w < State.Width; w++)
            {
                for (int h = 0; h < State.Height; h++)
                {
                    State.Cells[w, h].IsAlive = State.Cells[w, h].IsAliveNext;
                }
            }

            // TODO: When migrating to Redis, consider batching writes for better performance
            await WriteStateAsync(); 
        }

        public Task Clear()
        {
            throw new NotImplementedException();
        }

        public async Task<Cell[,]> GetChunk()
        {
            await ReadStateAsync();

            return this.State.Cells;
        }

        public Task SetChunk(Cell[][] value)
        {
            throw new NotImplementedException();
        }

        #region Inter-Chunk Communication Methods

        public async Task SetNeighborChunks(
            string topChunkId, string bottomChunkId,
            string leftChunkId, string rightChunkId,
            string topLeftChunkId, string topRightChunkId,
            string bottomLeftChunkId, string bottomRightChunkId)
        {
            await ReadStateAsync();

            State.TopChunkId = topChunkId;
            State.BottomChunkId = bottomChunkId;
            State.LeftChunkId = leftChunkId;
            State.RightChunkId = rightChunkId;
            State.TopLeftChunkId = topLeftChunkId;
            State.TopRightChunkId = topRightChunkId;
            State.BottomLeftChunkId = bottomLeftChunkId;
            State.BottomRightChunkId = bottomRightChunkId;

            await WriteStateAsync();
        }

        public async Task<bool[]> GetTopEdge()
        {
            await ReadStateAsync();
            var edge = new bool[State.Width];
            for (int x = 0; x < State.Width; x++)
            {
                edge[x] = State.Cells[x, 0].IsAlive;
            }
            return edge;
        }

        public async Task<bool[]> GetBottomEdge()
        {
            await ReadStateAsync();
            var edge = new bool[State.Width];
            for (int x = 0; x < State.Width; x++)
            {
                edge[x] = State.Cells[x, State.Height - 1].IsAlive;
            }
            return edge;
        }

        public async Task<bool[]> GetLeftEdge()
        {
            await ReadStateAsync();
            var edge = new bool[State.Height];
            for (int y = 0; y < State.Height; y++)
            {
                edge[y] = State.Cells[0, y].IsAlive;
            }
            return edge;
        }

        public async Task<bool[]> GetRightEdge()
        {
            await ReadStateAsync();
            var edge = new bool[State.Height];
            for (int y = 0; y < State.Height; y++)
            {
                edge[y] = State.Cells[State.Width - 1, y].IsAlive;
            }
            return edge;
        }

        public async Task<bool> GetTopLeftCorner()
        {
            await ReadStateAsync();
            return State.Cells[0, 0].IsAlive;
        }

        public async Task<bool> GetTopRightCorner()
        {
            await ReadStateAsync();
            return State.Cells[State.Width - 1, 0].IsAlive;
        }

        public async Task<bool> GetBottomLeftCorner()
        {
            await ReadStateAsync();
            return State.Cells[0, State.Height - 1].IsAlive;
        }

        public async Task<bool> GetBottomRightCorner()
        {
            await ReadStateAsync();
            return State.Cells[State.Width - 1, State.Height - 1].IsAlive;
        }

        #endregion

        #region Helper Methods for Cross-Chunk Communication

        private class NeighborEdgeData
        {
            public bool[] TopEdge { get; set; }
            public bool[] BottomEdge { get; set; }
            public bool[] LeftEdge { get; set; }
            public bool[] RightEdge { get; set; }
            public bool TopLeftCorner { get; set; }
            public bool TopRightCorner { get; set; }
            public bool BottomLeftCorner { get; set; }
            public bool BottomRightCorner { get; set; }
        }

        private async Task<NeighborEdgeData> FetchNeighborEdges()
        {
            var edgeData = new NeighborEdgeData();

            // Fetch edges from neighboring chunks if they exist
            if (!string.IsNullOrEmpty(State.TopChunkId))
            {
                var topChunk = GrainFactory.GetGrain<IGoLChunkGrain>(State.TopChunkId);
                edgeData.TopEdge = await topChunk.GetBottomEdge();
            }

            if (!string.IsNullOrEmpty(State.BottomChunkId))
            {
                var bottomChunk = GrainFactory.GetGrain<IGoLChunkGrain>(State.BottomChunkId);
                edgeData.BottomEdge = await bottomChunk.GetTopEdge();
            }

            if (!string.IsNullOrEmpty(State.LeftChunkId))
            {
                var leftChunk = GrainFactory.GetGrain<IGoLChunkGrain>(State.LeftChunkId);
                edgeData.LeftEdge = await leftChunk.GetRightEdge();
            }

            if (!string.IsNullOrEmpty(State.RightChunkId))
            {
                var rightChunk = GrainFactory.GetGrain<IGoLChunkGrain>(State.RightChunkId);
                edgeData.RightEdge = await rightChunk.GetLeftEdge();
            }

            // Fetch corners
            if (!string.IsNullOrEmpty(State.TopLeftChunkId))
            {
                var topLeftChunk = GrainFactory.GetGrain<IGoLChunkGrain>(State.TopLeftChunkId);
                edgeData.TopLeftCorner = await topLeftChunk.GetBottomRightCorner();
            }

            if (!string.IsNullOrEmpty(State.TopRightChunkId))
            {
                var topRightChunk = GrainFactory.GetGrain<IGoLChunkGrain>(State.TopRightChunkId);
                edgeData.TopRightCorner = await topRightChunk.GetBottomLeftCorner();
            }

            if (!string.IsNullOrEmpty(State.BottomLeftChunkId))
            {
                var bottomLeftChunk = GrainFactory.GetGrain<IGoLChunkGrain>(State.BottomLeftChunkId);
                edgeData.BottomLeftCorner = await bottomLeftChunk.GetTopRightCorner();
            }

            if (!string.IsNullOrEmpty(State.BottomRightChunkId))
            {
                var bottomRightChunk = GrainFactory.GetGrain<IGoLChunkGrain>(State.BottomRightChunkId);
                edgeData.BottomRightCorner = await bottomRightChunk.GetTopLeftCorner();
            }

            return edgeData;
        }

        private int CountCrossChunkNeighbors(int x, int y, bool isLeftEdge, bool isRightEdge, bool isTopEdge, bool isBottomEdge, NeighborEdgeData edgeData)
        {
            int crossChunkNeighbors = 0;

            // Top edge neighbors
            if (isTopEdge && edgeData.TopEdge != null)
            {
                if (!isLeftEdge && edgeData.TopEdge[x - 1]) crossChunkNeighbors++;
                if (edgeData.TopEdge[x]) crossChunkNeighbors++;
                if (!isRightEdge && edgeData.TopEdge[x + 1]) crossChunkNeighbors++;
            }

            // Bottom edge neighbors
            if (isBottomEdge && edgeData.BottomEdge != null)
            {
                if (!isLeftEdge && edgeData.BottomEdge[x - 1]) crossChunkNeighbors++;
                if (edgeData.BottomEdge[x]) crossChunkNeighbors++;
                if (!isRightEdge && edgeData.BottomEdge[x + 1]) crossChunkNeighbors++;
            }

            // Left edge neighbors
            if (isLeftEdge && edgeData.LeftEdge != null)
            {
                if (!isTopEdge && edgeData.LeftEdge[y - 1]) crossChunkNeighbors++;
                if (edgeData.LeftEdge[y]) crossChunkNeighbors++;
                if (!isBottomEdge && edgeData.LeftEdge[y + 1]) crossChunkNeighbors++;
            }

            // Right edge neighbors
            if (isRightEdge && edgeData.RightEdge != null)
            {
                if (!isTopEdge && edgeData.RightEdge[y - 1]) crossChunkNeighbors++;
                if (edgeData.RightEdge[y]) crossChunkNeighbors++;
                if (!isBottomEdge && edgeData.RightEdge[y + 1]) crossChunkNeighbors++;
            }

            // Corner neighbors
            if (isTopEdge && isLeftEdge && edgeData.TopLeftCorner) crossChunkNeighbors++;
            if (isTopEdge && isRightEdge && edgeData.TopRightCorner) crossChunkNeighbors++;
            if (isBottomEdge && isLeftEdge && edgeData.BottomLeftCorner) crossChunkNeighbors++;
            if (isBottomEdge && isRightEdge && edgeData.BottomRightCorner) crossChunkNeighbors++;

            return crossChunkNeighbors;
        }

        #endregion
    }
}