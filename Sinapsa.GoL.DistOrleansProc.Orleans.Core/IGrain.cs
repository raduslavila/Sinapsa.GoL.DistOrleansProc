using Orleans;
using System;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core
{
    public interface IGrain<TGrainIdentity> : IGrain where TGrainIdentity: IEquatable<TGrainIdentity>
    {
    }
}
