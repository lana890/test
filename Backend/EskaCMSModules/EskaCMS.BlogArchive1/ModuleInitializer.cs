
using EskaCMS.BlogArchive.Services;
using EskaCMS.BlogArchive.Services.Interfaces;

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
            services.AddTransient<IBlogArchive, BlogArchiveService>();
        

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
