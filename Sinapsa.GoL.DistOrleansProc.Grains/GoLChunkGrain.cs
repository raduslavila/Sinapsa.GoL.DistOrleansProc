using Orleans;
using Orleans.Providers;
using Orleans.Placement;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models;
using System.Diagnostics;

namespace Sinapsa.GoL.DistOrleansProc.Grains
{
    [StorageProvider(ProviderName = "GoLChunkStore")]
    [ActivationCountBasedPlacement]
    public class GoLChunkGrain : Grain<GoLChunkGrainState>, IGoLChunkGrain
    {
        private Random rand = new Random();

        private void ConnectNeighbors()
        {
            for (int x = 0; x < State.Size; x++)
            {
                rand = new Random();

                for (int y = 0; y < State.Size; y++)
                {
                    bool isLeftEdge = (x == 0);
                    bool isRightEdge = (x == State.Size - 1);
                    bool isTopEdge = (y == 0);
                    bool isBottomEdge = (y == State.Size - 1);

                    int xL = x - 1;
                    int xR = x + 1;
                    int yT = y - 1;
                    int yB = y + 1;

                    if (!isLeftEdge && !isTopEdge)
                    {
                        State.Cells[x, y].neighbors.Add(State.Cells[xL, yT]);
                    }
                    if (!isTopEdge)
                    {
                        State.Cells[x, y].neighbors.Add(State.Cells[x, yT]);
                    }
                    if (!isRightEdge && !isTopEdge)
                    {
                        State.Cells[x, y].neighbors.Add(State.Cells[xR, yT]);
                    }
                    if (!isLeftEdge)
                    {
                        State.Cells[x, y].neighbors.Add(State.Cells[xL, y]);
                    }
                    if (!isRightEdge)
                    {
                        State.Cells[x, y].neighbors.Add(State.Cells[xR, y]);
                    }
                    if (!isLeftEdge && !isBottomEdge)
                    {
                        State.Cells[x, y].neighbors.Add(State.Cells[xL, yB]);
                    }
                    if (!isBottomEdge)
                    {
                        State.Cells[x, y].neighbors.Add(State.Cells[x, yB]);
                    }
                    if (!isRightEdge && !isBottomEdge)
                    {
                        State.Cells[x, y].neighbors.Add(State.Cells[xR, yB]);
                    }
                }
            }
        }

        public async Task InitChunk(int chunkX, int chunkY, int size, double liveDensity)
        {
            this.State = new GoLChunkGrainState
            {
                ChunkId = this.GetPrimaryKeyString(),
                ChunkLocationX = chunkX,
                ChunkLocationY = chunkY,
                Size = size,
                Cells = new Cell[size, size]
            };

            for (int x = 0; x < this.State.Size; x++)
                for (int y = 0; y < this.State.Size; y++)
                    this.State.Cells[x, y] = new Cell();

            foreach (var cell in this.State.Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;

            await WriteStateAsync();
        }


        public async Task Advance()
        {
            await ReadStateAsync();

            // Fetch neighbor edge data for cross-chunk boundary cells
            var neighborEdges = await FetchNeighborEdges();

            for (int w = 0; w < State.Size; w++)
            {
                for (int h = 0; h < State.Size; h++)
                {
                    bool isLeftEdge = (w == 0);
                    bool isRightEdge = (w == State.Size - 1);
                    bool isTopEdge = (h == 0);
                    bool isBottomEdge = (h == State.Size - 1);

                    // Count neighbors within this chunk
                    int liveNeighbors = CountInternalNeighbors(w, h);

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
            var changedCells = Enumerable.Range(0, State.Size)
                .SelectMany(w => Enumerable.Range(0, State.Size)
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
            for (int w = 0; w < State.Size; w++)
            {
                for (int h = 0; h < State.Size; h++)
                {
                    State.Cells[w, h].IsAlive = State.Cells[w, h].IsAliveNext;
                }
            }

            // TODO: When migrating to Redis, consider batching writes for better performance
            await WriteStateAsync(); 
        }

        public async Task Clear()
        {
            // Reset all cells to dead state, keeping the chunk structure
            if (State?.Cells != null)
            {
                for (int x = 0; x < State.Size; x++)
                {
                    for (int y = 0; y < State.Size; y++)
                    {
                        State.Cells[x, y].IsAlive = false;
                        State.Cells[x, y].IsAliveNext = false;
                    }
                }

                await WriteStateAsync();
            }
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
            var edge = new bool[State.Size];
            for (int x = 0; x < State.Size; x++)
            {
                edge[x] = State.Cells[x, 0].IsAlive;
            }
            return edge;
        }

        public async Task<bool[]> GetBottomEdge()
        {
            await ReadStateAsync();
            var edge = new bool[State.Size];
            for (int x = 0; x < State.Size; x++)
            {
                edge[x] = State.Cells[x, State.Size - 1].IsAlive;
            }
            return edge;
        }

        public async Task<bool[]> GetLeftEdge()
        {
            await ReadStateAsync();
            var edge = new bool[State.Size];
            for (int y = 0; y < State.Size; y++)
            {
                edge[y] = State.Cells[0, y].IsAlive;
            }
            return edge;
        }

        public async Task<bool[]> GetRightEdge()
        {
            await ReadStateAsync();
            var edge = new bool[State.Size];
            for (int y = 0; y < State.Size; y++)
            {
                edge[y] = State.Cells[State.Size - 1, y].IsAlive;
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
            return State.Cells[State.Size - 1, 0].IsAlive;
        }

        public async Task<bool> GetBottomLeftCorner()
        {
            await ReadStateAsync();
            return State.Cells[0, State.Size - 1].IsAlive;
        }

        public async Task<bool> GetBottomRightCorner()
        {
            await ReadStateAsync();
            return State.Cells[State.Size - 1, State.Size - 1].IsAlive;
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

        private int CountInternalNeighbors(int x, int y)
        {
            var count = 0;

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    var nx = x + dx;
                    var ny = y + dy;

                    if (nx < 0 || nx >= State.Size || ny < 0 || ny >= State.Size)
                    {
                        continue;
                    }

                    if (State.Cells[nx, ny].IsAlive)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        #endregion
    }
}