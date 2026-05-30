using Orleans;
using Orleans.Core;
using System;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services
{
    public class GrainFactoryWithGuidIdentity<TGrain> : IGrainFactory<TGrain, Guid> where TGrain : IGrain<Guid>, IGrainWithGuidKey
    {
        private readonly IGrainFactory _grainFactory;

        public GrainFactoryWithGuidIdentity(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        public TGrain GetGrain(Guid grainIdentity)
        {
            return _grainFactory.GetGrain<TGrain>(grainIdentity);
        }
    }
}