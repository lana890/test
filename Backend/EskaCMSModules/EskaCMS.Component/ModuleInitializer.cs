

using EskaCMS.Components.Data;
using EskaCMS.Components.Services;
using EskaCMS.Components.Services.Interfaces;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.Module.Core.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;


namespace EskaCMS.Components
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IComponent, ComponentService>();
            //services.AddTransient<IDataSeeder, ComponentDataSeeder>();
            services.AddTransient<IComponentKeysServices, ComponentKeysServices>();
            services.AddTransient<IPredefinedTemplatesServices, PredefinedTemplatesServices>();
            services.AddTransient<IComponentPermissionServices, ComponentPermissionServices>();


        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
