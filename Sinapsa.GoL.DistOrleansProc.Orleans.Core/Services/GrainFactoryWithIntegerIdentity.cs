using Orleans;
using Orleans.Core;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services
{
    public class GrainFactoryWithIntegerIdentity<TGrain> : IGrainFactory<TGrain, long> where TGrain : IGrain<long>, IGrainWithIntegerKey
    {
        private readonly IGrainFactory _grainFactory;

        public GrainFactoryWithIntegerIdentity(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        public TGrain GetGrain(long grainIdentity)
        {
            return _grainFactory.GetGrain<TGrain>(grainIdentity);
        }

        public long GetIdentity(IGrainIdentity grainIdentity)
        {
            return grainIdentity.PrimaryKeyLong;
        }
    }
}