using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

using EskaCMS.Infrastructure.Modules;

using EskaCMS.Tax.Services;
using EskaCMS.Infrastructure.Data;

namespace EskaCommerce.Module.Tax
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<ITaxService, TaxService>();
            
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
        }
    }
}
