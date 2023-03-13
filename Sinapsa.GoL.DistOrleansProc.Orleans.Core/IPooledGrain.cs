using System;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core
{
    public interface IPooledGrain<TGrainIdentity> : IGrain<TGrainIdentity> where TGrainIdentity : IEquatable<TGrainIdentity>
    {
    }
}
