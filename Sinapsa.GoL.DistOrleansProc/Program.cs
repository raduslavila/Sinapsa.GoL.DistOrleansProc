using Microsoft.AspNetCore.Server.Kestrel.Core;
using Orleans;
using System.Runtime;
using Sinapsa.GoL.DistOrleansProc.Domain.Extensions;
using Orleans.Hosting;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces;
using Sinapsa.GoL.DistOrleansProc.Grains;
using Orleans.Configuration;

namespace Sinapsa.GoL.DistOrleansProc
{
    public class Program
    {

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseOrleans((context, siloBuilder) =>
                {
                    var config = context.Configuration;

                    siloBuilder.ConfigureCluster(config);

                    siloBuilder.ConfigureApplicationParts(parts =>
                         parts.AddApplicationPart(typeof(GoLChunkGrain).Assembly).WithReferences());

                    siloBuilder.ConfigureMemoryGrainStorage(typeof(GoLChunkGrain).Assembly);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .ConfigureKestrel((host, options) =>
                    {
                        // http port for controllers
                        options.ListenAnyIP(5050, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });
                    })
                    .UseStartup<Startup>();

                }).ConfigureServices(services =>
                {
                    services.AddControllers();

                    // Add CORS for React frontend
                    services.AddCors(options =>
                    {
                        options.AddPolicy("AllowReactApp", builder =>
                        {
                            builder.WithOrigins("http://localhost:3000")
                                   .AllowAnyMethod()
                                   .AllowAnyHeader();
                        });
                    });

                    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                    services.AddEndpointsApiExplorer();
                    services.AddSwaggerGen();
                });
    }
}