using Orleans;
using Orleans.Providers;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models;
using System.Diagnostics;

namespace Sinapsa.GoL.DistOrleansProc.Grains
{

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

        public async void InitChunk(int width, int height, double liveDensity)
        {
            this.State = new GoLChunkGrainState();

            this.State.Cells = new Cell[width, height];
            this.State.Width = width;
            this.State.Height = height;   

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

            var currentState = this.State;

            for (int w = 0; w < currentState.Width; w++)
            {
                for (int h = 0; h < currentState.Height; h++)
                {
                    //Any live cell with fewer than two live neighbours dies, as if by underpopulation.
                    //Any live cell with more than three live neighbours dies, as if by overpopulation.
                    //Any live cell with two or three live neighbours lives on to the next generation.
                    //Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction.

                    //if (!Cells[w, h].neighbors.Any(x => x.IsAlive))
                    //{
                    //    Cells[w, h].IsAliveNext = false;
                    //}
                    //else
                    //{
                    int liveNeighbors = currentState.Cells[w, h].neighbors.Count(x => x.IsAlive);

                    if (currentState.Cells[w, h].IsAlive)
                        currentState.Cells[w, h].IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
                    else
                        currentState.Cells[w, h].IsAliveNext = liveNeighbors == 3;
                    //}
                }
            }

            for (int w = 0; w < currentState.Width; w++)
            {
                for (int h = 0; h < currentState.Height; h++)
                {
                    currentState.Cells[w, h].IsAlive = currentState.Cells[w, h].IsAliveNext;
                }
            }

            await WriteStateAsync(); 
        }

        public Task Clear()
        {
            throw new NotImplementedException();
        }

        public async Task<Cell[,]> GetChunk()
        {
            await ReadStateAsync();

            var currentState = this.State;

            return currentState.Cells;
        }

        public Task SetChunk(Cell[][] value)
        {
            throw new NotImplementedException();
        }
    }
}