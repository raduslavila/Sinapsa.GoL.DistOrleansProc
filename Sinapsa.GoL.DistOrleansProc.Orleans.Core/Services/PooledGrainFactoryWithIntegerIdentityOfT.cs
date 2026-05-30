using Orleans;
using Orleans.Core;
using System;
using System.Collections.Concurrent;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services
{
    public class PooledGrainFactoryWithIntegerIdentityOfT<TGrain> : IPooledGrainFactory<TGrain, long> where TGrain : class, IPooledGrain<long>, IGrainWithIntegerCompoundKey
    {
        private readonly IGrainFactory _grainFactory;
        private readonly ConcurrentDictionary<long, ConcurrentStack<TGrain>> _workers = new ConcurrentDictionary<long, ConcurrentStack<TGrain>>();

        public PooledGrainFactoryWithIntegerIdentityOfT(IGrainFactory grainFactory)
        {
            this._grainFactory = grainFactory;
        }

        private ConcurrentStack<TGrain> CreateWorkerPool(long grainIdentity) => new ConcurrentStack<TGrain>();

        private void Release(TGrain treeContextGrain)
        {
            var clientWorkers = _workers.GetOrAdd(treeContextGrain.GetPrimaryKeyLong(out _), CreateWorkerPool);
            clientWorkers.Push(treeContextGrain);
        }

        public IRentedGrain<TGrain, long> RentGrain(long grainIdentity)
        {
            var clientWorkers = _workers.GetOrAdd(grainIdentity, CreateWorkerPool);
            if (!clientWorkers.TryPop(out var treeContext))
            {
                treeContext = _grainFactory.GetGrain<TGrain>(grainIdentity, Guid.NewGuid().ToString());
            }
            return new RentedGrain<TGrain, long>(treeContext, this.Release);
        }
    }
}
