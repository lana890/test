using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.Infrastructure.Data;
using Microsoft.Extensions.Localization;
using EskaCMS.Localization.Services;
using EskaCMS.Localization.Services.Interfaces;

namespace EskaCMS.Localization
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {

            serviceCollection.AddTransient<ILocalizationServices, LocalizationServices>();
            
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
