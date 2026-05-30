using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
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

        Task InitChunk(int chunkX, int chunkY, int size, double liveDensity);

        Task SetChunk(Cell[][] value);

        Task Advance();

        Task Clear();

        // Methods for inter-chunk communication
        [AlwaysInterleave]
        Task<bool[]> GetTopEdge();

        [AlwaysInterleave]
        Task<bool[]> GetBottomEdge();

        [AlwaysInterleave]
        Task<bool[]> GetLeftEdge();

        [AlwaysInterleave]
        Task<bool[]> GetRightEdge();

        [AlwaysInterleave]
        Task<bool> GetTopLeftCorner();

        [AlwaysInterleave]
        Task<bool> GetTopRightCorner();

        [AlwaysInterleave]
        Task<bool> GetBottomLeftCorner();

        [AlwaysInterleave]
        Task<bool> GetBottomRightCorner();

        Task SetNeighborChunks(
            string topChunkId, string bottomChunkId,
            string leftChunkId, string rightChunkId,
            string topLeftChunkId, string topRightChunkId,
            string bottomLeftChunkId, string bottomRightChunkId);
    }
}
