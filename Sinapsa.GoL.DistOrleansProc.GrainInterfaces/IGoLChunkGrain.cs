using Orleans;
using Orleans.Concurrency;
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
        Task<byte[][]> GetChunk();

        Task SetChunk(byte[][] value, TimeSpan deactivationDelay = default);

        Task Advance();

        Task Clear();
    }
}
