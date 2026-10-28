using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using EskaCMS.Infrastructure.Modules;
using EskaCommerce.Inventory.Services;
using EskaCMS.Infrastructure;
using EskaCommerce.Inventory.Services.Interface;

namespace EskaCommerce.Module.Inventory
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IStockService, StockService>();
            serviceCollection.AddTransient<IWarehouseServices, WarehouseService>();

            GlobalConfiguration.RegisterAngularModule("simplAdmin.inventory");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
