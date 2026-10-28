using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.Core.Services;
using EskaCMS.StorageLocal;
using EskaCMS.Module.Core;

namespace EskaCMS.Module.StorageLocal
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
         //  serviceCollection.AddSingleton<IStorageService, LocalStorageService>();
         
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
        }
    }
}
