using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Statistics;
using Sinapsa.GoL.DistOrleansProc.Domain.Configuration;
using Sinapsa.GoL.DistOrleansProc.Domain.Services;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces;
using Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services;
using System.Reflection;

namespace Sinapsa.GoL.DistOrleansProc.Domain.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGoLDistributedOrleansGrainFactories(this IServiceCollection services)
        {
            // GoLService uses Orleans' built-in IGrainFactory (provided by the silo host) to
            // resolve IGoLUniverseGrain.  The IClusterClient in-process is the IGrainFactory.
            services.AddSingleton<IGrainFactory>(q => q.GetRequiredService<IClusterClient>());

            services.AddSingleton<IGoLService, GoLService>();

            return services;
        }
    }
}
