using Orleans;
using Orleans.Core;
using System;
using System.Collections.Concurrent;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services
{
    public class PooledGrainFactoryWithDefaultIdentityOfT<TGrain> : IPooledGrainFactory<TGrain, DefaultIdentity> where TGrain : class, IPooledGrain<DefaultIdentity>, IGrainWithGuidKey
    {
        private readonly IGrainFactory _grainFactory;
        private readonly ConcurrentStack<TGrain> _workers = new ConcurrentStack<TGrain>();

        public PooledGrainFactoryWithDefaultIdentityOfT(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        private void Release(TGrain treeContextGrain)
        {
            _workers.Push(treeContextGrain);
        }

        public IRentedGrain<TGrain, DefaultIdentity> RentGrain(DefaultIdentity _)
        {
            if (!_workers.TryPop(out var treeContext))
            {
                treeContext = _grainFactory.GetGrain<TGrain>(Guid.NewGuid());
            }
            return new RentedGrain<TGrain, DefaultIdentity>(treeContext, this.Release);
        }
    }
}
