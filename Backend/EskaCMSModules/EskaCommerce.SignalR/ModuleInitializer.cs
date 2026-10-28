using EskaCMS.Infrastructure.Modules;
using EskaCMS.SignalR.Hubs;
using EskaCMS.SignalR.RealTime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;


namespace EskaCommerce.SignalR
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
          //  serviceCollection.AddSignalR();
            serviceCollection.AddSingleton<IOnlineClientManager, OnlineClientManager>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            app.UseEndpoints(routes =>
            {
                routes.MapHub<CommonHub>("/locationStreamingSocket");
            });
        }
    }
}
