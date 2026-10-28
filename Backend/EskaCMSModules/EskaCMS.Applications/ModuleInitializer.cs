


using EskaCMS.Infrastructure.Modules;
using EskaCMS.Sites.Helpers;
using EskaCMS.Sites.Services;
using EskaCMS.Sites.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;

namespace EskaCMS.Sites
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ISites, SitesService>();
            services.AddTransient<ISiteSlugServices, SiteSlugServices>();
            services.AddTransient<ISitePreferance, SitePreferanceService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
