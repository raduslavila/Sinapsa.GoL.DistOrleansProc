using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Statistics;
using Sinapsa.GoL.DistOrleansProc.Domain.Configuration;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces;
using System.Reflection;

namespace Sinapsa.GoL.DistOrleansProc.Domain.Extensions
{
    public static class SiloHostBuilderExtensions
    {
        public static ISiloBuilder ConfigureCluster(this ISiloBuilder siloBuilder, IConfiguration configuration)
        {
            return siloBuilder.ConfigureCluster(configuration, null);
        }

        public static ISiloBuilder ConfigureCluster(this ISiloBuilder siloBuilder, IConfiguration configuration, Action<ClusterConfig> clusterOptionsBuilder)
        {
            if (configuration == null && clusterOptionsBuilder == null) throw new ArgumentException($"The {nameof(configuration)} cannot be null");
            siloBuilder.ConfigureServices(services =>
                services.AddSingleton<PlacementStrategy, PreferLocalPlacement>());
            var clusterConfig = new ClusterConfig();
            if (configuration != null)
            {
                var clusterConfigSection = configuration.GetSection(nameof(ClusterConfig));
                if (clusterConfigSection.Exists())
                {
                    clusterConfigSection.Bind(clusterConfig);
                }
            }
            clusterOptionsBuilder?.Invoke(clusterConfig);







            if (clusterConfig.UseDashboard)
            {
                siloBuilder.UseDashboard(o =>
                {
                    o.CounterUpdateIntervalMs = 5000;
                    o.HideTrace = false;
                    o.BasePath = clusterConfig.DashboardPath ?? "dashboard";
                    if (clusterConfig.DashboardPort.HasValue)
                    {
                        o.Port = clusterConfig.DashboardPort.Value;
                    }
                });
            }

            if (clusterConfig.UseKubernetesHosting)
            {
                siloBuilder.UseKubernetesHosting();
            }
            else
            {
                siloBuilder.Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = clusterConfig.ClusterOptions.ClusterId;
                    options.ServiceId = clusterConfig.ClusterOptions.ServiceId;
                });
            }

            if (clusterConfig.UseLocalhost)
            {
                siloBuilder.UseLocalhostClustering(clusterConfig.ClusterEndpointOptions?.GatewayPort ?? 11111, clusterConfig.ClusterEndpointOptions?.SiloPort ?? 30000);
            }
            else
            {
                if (clusterConfig.UseLinuxStatistics)
                {
                    siloBuilder.UseLinuxEnvironmentStatistics();
                }

                if (!clusterConfig.UseKubernetesHosting)
                {
                    siloBuilder.Configure<EndpointOptions>(options =>
                    {
                        options.AdvertisedIPAddress = clusterConfig.ClusterEndpointOptions.AdvertisedIPAddress;
                        options.GatewayListeningEndpoint = clusterConfig.ClusterEndpointOptions.GatewayListeningEndpoint;
                        options.GatewayPort = clusterConfig.ClusterEndpointOptions.GatewayPort;
                        options.SiloListeningEndpoint = clusterConfig.ClusterEndpointOptions.SiloListeningEndpoint;
                        options.SiloPort = clusterConfig.ClusterEndpointOptions.SiloPort;
                    });
                }
            }

            return siloBuilder;
        }

        public static ISiloBuilder ConfigureMemoryGrainStorage(this ISiloBuilder siloBuilder, params Assembly[] assemblies)
        {
            var requiredProviders = new HashSet<string>();
            foreach (Type type in assemblies.SelectMany(q => q.GetTypes()).Where(q => typeof(Grain).IsAssignableFrom(q)))
            {
                var storageProvider = type.GetCustomAttribute<StorageProviderAttribute>(true);
                if (storageProvider != null)
                {
                    requiredProviders.Add(storageProvider.ProviderName);
                }
                var ctors = type.GetConstructors();
                if (ctors.Length == 1)
                {
                    var ctor = ctors[0];
                    foreach (var arg in ctor.GetParameters())
                    {
                        var persistantState = arg.GetCustomAttribute<PersistentStateAttribute>(true);
                        if (persistantState != null)
                        {
                            requiredProviders.Add(persistantState.StorageName);
                        }
                    }
                }
            }
            siloBuilder.AddMemoryGrainStorage("PubSubStore");
            foreach (var requiredProvider in requiredProviders)
            {
                siloBuilder.AddMemoryGrainStorage(requiredProvider);
            }
            return siloBuilder;
        }


    }
}
