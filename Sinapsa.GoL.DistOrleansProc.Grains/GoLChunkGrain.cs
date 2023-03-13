using Orleans;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces;

namespace Sinapsa.GoL.DistOrleansProc.Grains
{
    public class GoLChunkGrain : Grain, IGoLChunkGrain
    {
        public Task Advance()
        {
            throw new NotImplementedException();
        }

        public Task Clear()
        {
            throw new NotImplementedException();
        }

        public Task<byte[][]> GetChunk()
        {
            throw new NotImplementedException();
        }

        public Task SetChunk(byte[][] value, TimeSpan deactivationDelay = default)
        {
            throw new NotImplementedException();
        }
    }
}