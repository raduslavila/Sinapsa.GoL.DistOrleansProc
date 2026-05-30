using Orleans.Dashboard;
using Sinapsa.GoL.DistOrleansProc.Domain.Configuration;
using Sinapsa.GoL.DistOrleansProc.Domain.Extensions;

namespace Sinapsa.GoL.DistOrleansProc
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(loggingBuilder => loggingBuilder.AddConsole());

            services.AddGoLDistributedOrleansGrainFactories();

            services.AddHealthChecks();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            // Enable CORS
            app.UseCors("AllowReactApp");

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/hc");

                var clusterConfig = new ClusterConfig();
                Configuration.GetSection(nameof(ClusterConfig)).Bind(clusterConfig);
                if (clusterConfig.UseDashboard)
                {
                    var routePrefix = "/" + (clusterConfig.DashboardPath ?? "dashboard").Trim('/');
                    endpoints.MapOrleansDashboard(routePrefix);
                }
            });
        }
    }
}
