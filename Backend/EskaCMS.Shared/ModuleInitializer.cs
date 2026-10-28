using EskaCMS.ADIntegration.Interfaces;
using EskaCMS.ADIntegration.Services;
using EskaCMS.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;


namespace EskaCMS.Shared.Core
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {

            serviceCollection.AddTransient<IADService, ADService>();
            // Confirmed unused: IEskaCoreIntegrationService has no consumer anywhere in the solution
            // (only its interface declaration, its implementation, and this registration).
            // serviceCollection.AddTransient<IEskaCoreIntegrationService, EskaCoreIntegrationService>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
