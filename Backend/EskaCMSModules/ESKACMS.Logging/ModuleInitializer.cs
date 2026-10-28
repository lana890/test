using EskaCMS.Core.Extensions;
using EskaCMS.Core.Extentions.Settings;
using EskaCMS.Core.Repositories;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;


namespace EskaCMS.Module.Logging
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            
            serviceCollection.AddScoped<IApiLogService, ApiLogService>();
            
          
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
