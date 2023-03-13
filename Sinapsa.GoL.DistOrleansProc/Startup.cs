using Sinapsa.GoL.DistOrleansProc.GrainInterfaces;
using Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services;

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

            services.AddSingleton<IGrainFactory<IGoLChunkGrain, string>, GrainFactoryWithStringIdentity<IGoLChunkGrain>>();

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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/hc");
            });
        }
    }
}
