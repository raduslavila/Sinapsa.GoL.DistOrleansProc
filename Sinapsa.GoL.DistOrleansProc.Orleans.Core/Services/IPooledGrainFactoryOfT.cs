using Orleans.Core;
using System;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services
{
    public interface IPooledGrainFactory<out TGrain, TGrainIdentity> where TGrain : IPooledGrain<TGrainIdentity> where TGrainIdentity : IEquatable<TGrainIdentity>
    {
        IRentedGrain<TGrain, TGrainIdentity> RentGrain(TGrainIdentity grainIdentity);
    }
}
