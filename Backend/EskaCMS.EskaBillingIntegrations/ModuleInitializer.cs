using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using EskaCMS.Infrastructure.Modules;
using EskaCMS.EskaBilling.Services;
using EskaCMS.EskaBilling.Services.Interfaces;
using EskaCMS.EskaBilling.Configurations.Interface;
using EskaCMS.EskaBilling.Configurations;

namespace EskaCMS.Integrations.EskaBilling
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IIOTService, IOTService>();
            services.AddTransient<ISIMsActionsService, SIMsActionsService>();
            // Confirmed unused: ICHARTServices has no consumer anywhere in the solution
            // (only its interface declaration, its implementation, and this registration).
            // services.AddTransient<ICHARTServices, CHARTServices>();
            services.AddTransient<IConfigProvider, ConfigProvider>();
            // Confirmed unused: IBillingServices has no consumer anywhere in the solution
            // (only its interface declaration, its implementation, and this registration).
            // services.AddTransient<IBillingServices, BillingServices>();
         //   services.AddHostedService<CustomerSimsBackgroundService>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
