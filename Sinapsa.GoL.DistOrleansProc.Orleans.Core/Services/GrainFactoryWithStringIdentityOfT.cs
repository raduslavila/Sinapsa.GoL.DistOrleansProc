using Orleans;
using Orleans.Core;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services
{
    public class GrainFactoryWithStringIdentity<TGrain> : IGrainFactory<TGrain, string> where TGrain : IGrain<string>, IGrainWithStringKey
    {
        private readonly IGrainFactory _grainFactory;

        public GrainFactoryWithStringIdentity(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        public TGrain GetGrain(string grainIdentity)
        {
            return _grainFactory.GetGrain<TGrain>(grainIdentity);
        }
    }
}