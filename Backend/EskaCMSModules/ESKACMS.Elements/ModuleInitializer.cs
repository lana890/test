
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Extentions.Settings;
using EskaCMS.Elements.Data;
using EskaCMS.Elements.Services;

using EskaCMS.Entities.Entity;
using EskaCMS.Elements.Services.IServices;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.Module.Core.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using EskaCMS.Entities.Entity.Elements;


namespace EskaCMS.Elements
{ 
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<Repository<ShareableElement>>();
            services.AddScoped<IElementService, ElementService>();
            
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
