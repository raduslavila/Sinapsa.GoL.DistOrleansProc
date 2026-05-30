using Orleans.Core;
using System;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services
{
    public interface IGrainFactory<out TGrain, TGrainIdentity>
        where TGrain : IGrain<TGrainIdentity>
        where TGrainIdentity: IEquatable<TGrainIdentity>
    {
        TGrain GetGrain(TGrainIdentity grainIdentity);
    }
}