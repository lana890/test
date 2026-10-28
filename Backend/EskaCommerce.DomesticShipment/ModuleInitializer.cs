

using EskaCMS.Infrastructure.Modules;
using EskaCommerce.DomesticShipment.SimsCreation.Services;
using EskaCommerce.DomesticShipment.SimsCreation.Services.Interfaces;
using EskaCommerce.DomesticShipment.TopDelivery.Services;
using EskaCommerce.DomesticShipment.TopDelivery.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EskaCommerce.DomesticShipment
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ISimsCreationServices, SimsCreationServices>();
            services.AddTransient<ITopDeliveryServices, TopDeliveryServices>();
        }

        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
        }
    }

}
