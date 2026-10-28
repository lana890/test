
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Extentions.Settings;


using EskaCMS.Infrastructure.Modules;
using EskaCMS.Pages.Services;
using EskaCMS.Pages.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;


namespace EskaCMS.Pages
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IPages, PagesService>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
