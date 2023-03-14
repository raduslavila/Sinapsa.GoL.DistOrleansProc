using Orleans;
using Orleans.Concurrency;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models;
using Sinapsa.GoL.DistOrleansProc.Orleans.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sinapsa.GoL.DistOrleansProc.GrainInterfaces
{
    public interface IGoLChunkGrain : IGrainWithStringKey, IGrain<string>
    {
        [AlwaysInterleave]
        Task<Cell[,]> GetChunk();

        void InitChunk(int width, int height, double liveDensity);

        Task SetChunk(Cell[][] value);

        Task Advance();

        Task Clear();
    }
}
