using System;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services
{
    public class RentedGrain<TGrain, TGrainIdentity> : IRentedGrain<TGrain, TGrainIdentity> where TGrain : class, IPooledGrain<TGrainIdentity> where TGrainIdentity: IEquatable<TGrainIdentity>
    {
        private readonly Action<TGrain> _releaseAction;
        private TGrain grain;

        public TGrain Grain { get => grain ?? throw new ObjectDisposedException(typeof(RentedGrain<TGrain, TGrainIdentity>).Name); private set => grain = value; }

        public RentedGrain(TGrain grain, Action<TGrain> releaseAction)
        {
            _releaseAction = releaseAction;
            Grain = grain;
        }

        public void Dispose()
        {
            if (Grain == null)
            {
                throw new ObjectDisposedException(nameof(RentedGrain<TGrain, TGrainIdentity>));
            }
            _releaseAction(Grain);
            Grain = null;
        }
    }
}
