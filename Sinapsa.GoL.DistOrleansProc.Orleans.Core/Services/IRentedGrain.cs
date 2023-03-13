using System;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services
{
    public interface IRentedGrain<out TGrain, TGrainIdentity> : IDisposable where TGrain : IPooledGrain<TGrainIdentity> where TGrainIdentity: IEquatable<TGrainIdentity>
    {
        TGrain Grain { get; }
    }
}
