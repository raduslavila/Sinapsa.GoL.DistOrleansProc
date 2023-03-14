using Orleans;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models;
using System.Diagnostics;

namespace Sinapsa.GoL.DistOrleansProc.Grains
{
    public class GoLChunkGrain : Grain, IGoLChunkGrain
    {
        private Cell[,] _cells;

        public int Columns { get { return _cells.GetLength(0); } }
        public int Rows { get { return _cells.GetLength(1); } }
        public int Width { get { return Columns; } }
        public int Height { get { return Rows; } }

        private Random rand = new Random();

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                rand = new Random();

                for (int y = 0; y < Rows; y++)
                {
                    bool isLeftEdge = (x == 0);
                    bool isRightEdge = (x == Columns - 1);
                    bool isTopEdge = (y == 0);
                    bool isBottomEdge = (y == Rows - 1);
                    //bool isEdge = isLeftEdge | isRightEdge | isTopEdge | isBottomEdge;

                    //if (isEdge)
                    //    continue;

                    int xL = x - 1;
                    int xR = x + 1;
                    int yT = y - 1;
                    int yB = y + 1;

                    if (!isLeftEdge && !isTopEdge)
                    {
                        _cells[x, y].neighbors.Add(_cells[xL, yT]);
                    }
                    if (!isTopEdge)
                    {
                        _cells[x, y].neighbors.Add(_cells[x, yT]);
                    }
                    if (!isRightEdge && !isTopEdge)
                    {
                        _cells[x, y].neighbors.Add(_cells[xR, yT]);
                    }
                    if (!isLeftEdge)
                    {
                        _cells[x, y].neighbors.Add(_cells[xL, y]);
                    }
                    if (!isRightEdge)
                    {
                        _cells[x, y].neighbors.Add(_cells[xR, y]);
                    }
                    if (!isLeftEdge && !isBottomEdge)
                    {
                        _cells[x, y].neighbors.Add(_cells[xL, yB]);
                    }
                    if (!isBottomEdge)
                    {
                        _cells[x, y].neighbors.Add(_cells[x, yB]);
                    }
                    if (!isRightEdge && !isBottomEdge)
                    {
                        _cells[x, y].neighbors.Add(_cells[xR, yB]);
                    }
                }
            }
        }

        public void InitChunk(int width, int height, double liveDensity)
        {
            _cells = new Cell[width, height];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    _cells[x, y] = new Cell();

            foreach (var cell in _cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;

            ConnectNeighbors();
        }


        public async Task Advance()
        {
            var opts = new ParallelOptions();
            opts.MaxDegreeOfParallelism = Process.GetCurrentProcess().Threads.Count;
            opts.TaskScheduler = TaskScheduler.Current;

            for (int w = 0; w < Columns; w++)
            {
                for (int h = 0; h < Rows; h++)
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
                    int liveNeighbors = _cells[w, h].neighbors.Count(x => x.IsAlive);

                    if (_cells[w, h].IsAlive)
                        _cells[w, h].IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
                    else
                        _cells[w, h].IsAliveNext = liveNeighbors == 3;
                    //}
                }
            }

            for (int w = 0; w < Columns; w++)
            {
                for (int h = 0; h < Rows; h++)
                {
                    _cells[w, h].IsAlive = _cells[w, h].IsAliveNext;
                }
            }

            //Parallel.For(0, Columns, opts, w =>
            //{
            //    Parallel.For(0, Rows, opts, h =>
            //    {
            //        //Any live cell with fewer than two live neighbours dies, as if by underpopulation.
            //        //Any live cell with more than three live neighbours dies, as if by overpopulation.
            //        //Any live cell with two or three live neighbours lives on to the next generation.
            //        //Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction.

            //        //if (!Cells[w, h].neighbors.Any(x => x.IsAlive))
            //        //{
            //        //    Cells[w, h].IsAliveNext = false;
            //        //}
            //        //else
            //        //{
            //        int liveNeighbors = _cells[w, h].neighbors.Count(x => x.IsAlive);

            //        if (_cells[w, h].IsAlive)
            //            _cells[w, h].IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            //        else
            //            _cells[w, h].IsAliveNext = liveNeighbors == 3;
            //        //}
            //    });
            //});

            ////important for this 2 foreachs to be one after another to determine the fullstateBoard
            //Parallel.For(0, Columns, opts, w =>
            //{
            //    Parallel.For(0, Rows, opts, h =>
            //    {
            //        _cells[w, h].IsAlive = _cells[w, h].IsAliveNext;
            //    });
            //});
        }

        public Task Clear()
        {
            throw new NotImplementedException();
        }

        public async Task<Cell[,]> GetChunk()
        {
            return _cells;
        }

        public Task SetChunk(Cell[][] value)
        {
            throw new NotImplementedException();
        }
    }
}