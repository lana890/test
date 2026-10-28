using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using EskaCMS.Infrastructure.Modules;
using EskaCommerce.ShippingPrices.Services;
using EskaCommerce.ShippingTableRate.Services;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.ShippingTableRate.Data;
using EskaCommerce.ShippingTableRate.Services.Interface;

namespace EskaCommerce.ShippingTableRate
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IShippingPriceServiceProvider, TableRateShippingServiceProvider>();
            services.AddTransient<IShippingTableRateServices, ShippingTableRateServices>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.shipping-tablerate");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
