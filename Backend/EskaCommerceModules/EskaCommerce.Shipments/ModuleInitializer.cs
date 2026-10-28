
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Modules;
using EskaCommerce.Aramex.Services;
using EskaCommerce.Aramex.Services.Interfaces;
using EskaCommerce.DomesticShipment.SimsCreation.Services;
using EskaCommerce.DomesticShipment.SimsCreation.Services.Interfaces;
using EskaCommerce.DomesticShipment.TopDelivery.Services;
using EskaCommerce.DomesticShipment.TopDelivery.Services.Interfaces;
using EskaCommerce.Shipments.Services.ShippingJeeblyService.Interfaces;
using EskaCommerce.Shipments.Services.ShippingPricesService.Interfaces;
using EskaCommerce.Shipments.Services.Shippment;
using EskaCommerce.Shipments.Services.Shippment.Interfaces;
using EskaCommerce.ShippingFree.Services;
using EskaCommerce.ShippingJeebly.Services;
using EskaCommerce.ShippingPrices.Services;
using EskaCommerce.ShippingTableRate.Services;
using EskaCommerce.ShippingTableRate.Services.Interface;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace EskaCommerce.Shipments
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
          services.AddTransient<IShipmentService, ShipmentService>();
          services.AddTransient<IDimensionService, DimensionService>();
            services.AddTransient<IShippingPriceServiceProvider, FreeShippingServiceProvider>();
            services.AddScoped<IJeeblyService, JeeblyService>();
            services.AddTransient<IShippingPriceService, ShippingPriceService>();
            services.AddTransient<IShippingPriceServiceProvider, TableRateShippingServiceProvider>();
            services.AddTransient<IShippingTableRateServices, ShippingTableRateServices>();
            services.AddTransient<ISimsCreationServices, SimsCreationServices>();
            services.AddTransient<ITopDeliveryServices, TopDeliveryServices>();
            services.AddTransient<IAramexShippingService, AramexShippingService>();
            services.AddTransient<IIntagrateDCMSFeatures, IntegrateDCMSFeatures>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.shippings");
            GlobalConfiguration.RegisterAngularModule("simplAdmin.shipping-tablerate");
            GlobalConfiguration.RegisterAngularModule("simplAdmin.shipment");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
