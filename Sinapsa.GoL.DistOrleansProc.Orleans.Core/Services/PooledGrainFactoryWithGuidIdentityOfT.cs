using Orleans;
using Orleans.Core;
using System;
using System.Collections.Concurrent;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services
{
    public class PooledGrainFactoryWithGuidIdentityOfT<TGrain> : IPooledGrainFactory<TGrain, Guid>  where TGrain : class, IPooledGrain<Guid>, IGrainWithGuidCompoundKey
    {
        private readonly IGrainFactory _grainFactory;
        private readonly ConcurrentDictionary<Guid, ConcurrentStack<TGrain>> _workers = new ConcurrentDictionary<Guid, ConcurrentStack<TGrain>>();

        public PooledGrainFactoryWithGuidIdentityOfT(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        private ConcurrentStack<TGrain> CreateWorkerPool(Guid grainIdentity) => new ConcurrentStack<TGrain>();

        private void Release(TGrain treeContextGrain)
        {
            var clientWorkers = _workers.GetOrAdd(treeContextGrain.GetPrimaryKey(out _), CreateWorkerPool);
            clientWorkers.Push(treeContextGrain);
        }

        public IRentedGrain<TGrain, Guid> RentGrain(Guid grainIdentity)
        {
            var clientWorkers = _workers.GetOrAdd(grainIdentity, CreateWorkerPool);
            if (!clientWorkers.TryPop(out var treeContext))
            {
                treeContext = _grainFactory.GetGrain<TGrain>(grainIdentity, Guid.NewGuid().ToString());
            }
            return new RentedGrain<TGrain, Guid>(treeContext, this.Release);
        }

        public Guid GetIdentity(IGrainIdentity grainIdentity)
        {
            return grainIdentity.GetPrimaryKey(out _);
        }
    }
}
