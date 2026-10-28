using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

using EskaCMS.Infrastructure.Modules;
using EskaCMS.Theming.Services.Interfaces;
using EskaCMS.Theming.Services;

namespace EskaCommerce.Module.Tax
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<ITheme, ThemeService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
        }
    }
}
