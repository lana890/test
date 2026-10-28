
using EskaCMS.Categories.Services;
using EskaCMS.Categories.Services.Interfaces;


using EskaCMS.Infrastructure.Modules;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;


namespace EskaCMS.BlogArchive
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IPostCategory, CategoriesService>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
