using Orleans;
using System;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core
{
    [Alias("Sinapsa.GoL.DistOrleansProc.Orleans.Core.IGrain`1")]
    public interface IGrain<TGrainIdentity> : IGrain where TGrainIdentity: IEquatable<TGrainIdentity>
    {
    }
}
