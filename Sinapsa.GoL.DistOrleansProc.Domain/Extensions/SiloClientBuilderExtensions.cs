using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting.Kubernetes;
using Sinapsa.GoL.DistOrleansProc.Domain.Configuration;
using Sinapsa.GoL.DistOrleansProc.Domain.Exceptions;

namespace Sinapsa.GoL.DistOrleansProc.Domain.Extensions
{
    public static class SiloClientBuilderExtensions
    {
        public static IServiceCollection ConfigureOrleansClient(this IServiceCollection services, Action<IClientBuilder> options, IConfiguration configuration)
        {
            return services.ConfigureOrleansClient(options, configuration, null);
        }

        public static IServiceCollection ConfigureOrleansClient(this IServiceCollection services, Action<IClientBuilder> options, Action<ClientConfig> optionsBuilder)
        {
            return services.ConfigureOrleansClient(options, null, optionsBuilder);
        }

        public static IServiceCollection ConfigureOrleansClient(this IServiceCollection services, Action<IClientBuilder> options,
            IConfiguration configuration, Action<ClientConfig> optionsBuilder)
        {
            if (configuration == null && optionsBuilder == null) throw new ArgumentException($"The {nameof(configuration)} and {nameof(optionsBuilder)} parameters cannot be null at the same time");
            var clientConfig = new ClientConfig();
            if (configuration != null)
            {
                var clientConfigSection = configuration.GetSection(nameof(ClientConfig));
                if (clientConfigSection.Exists())
                {
                    clientConfigSection.Bind(clientConfig);
                }
            }
            optionsBuilder?.Invoke(clientConfig);
            return services.AddSingleton<IGrainFactory>(q => q.GetRequiredService<IClusterClient>())
                .AddSingleton(q =>
                {
                    var client = new ClientBuilder();
                    if (clientConfig.UseKubernetesHosting)
                    {
                        client.Configure<ClusterOptions>(options =>
                        {
                            options.ServiceId = Environment.GetEnvironmentVariable(KubernetesHostingOptions.ServiceIdEnvironmentVariable);
                            options.ClusterId = Environment.GetEnvironmentVariable(KubernetesHostingOptions.ClusterIdEnvironmentVariable);
                        });
                    }
                    else
                    {
                        client.Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = clientConfig.ClusterOptions.ClusterId;
                            options.ServiceId = clientConfig.ClusterOptions.ServiceId;
                        });
                    }

                    if (clientConfig.UseLocalhost)
                    {
                        client.UseLocalhostClustering();
                    }
                    else
                    {
                        throw new ClusteringStorageProviderNotFoundException();
                    }
                    options(client);
                    return client.Build();
                });
        }

        public static Task StartClientWithRetries(this IHost host, int numberOfRetries = 5, int retryWindowInSeconds = 2) =>
            StartClientWithRetries(host.Services, numberOfRetries, retryWindowInSeconds);

        public static async Task StartClientWithRetries(this IServiceProvider serviceProvider, int numberOfRetries = 5, int retryWindowInSeconds = 2)
        {
            var clusterClient = serviceProvider.GetRequiredService<IClusterClient>();
            var logger = serviceProvider.GetRequiredService<ILogger<IClusterClient>>();
            try
            {
                logger.LogInformation("Trying to connect to cluster...");
                await clusterClient.Connect(async q =>
                {
                    logger.LogError(q, "Couldn't connect to cluster, trying again in {seconds} seconds", retryWindowInSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(retryWindowInSeconds));
                    return --numberOfRetries > 0;
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Couldn't connect to cluster");
                throw;
            }
        }
    }
}
