using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity.Core;

namespace EskaCommerce.ShippingJeebly.Data
{
    public class ShippingJeeblyCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //  modelBuilder.Entity<ShippingProvider>().HasSeedData(new ShippingProvider("Jeebly") { Name = "Jeebly", IsEnabled = true, ConfigureUrl = "shipping-Jeebly-rate-config", ShippingPriceServiceTypeName = "EskaCommerce.ShippingTableRate.Services.TableRateShippingServiceProvider,EskaCommerce.ShippingTableRate", AdditionalSettings = null, ToAllShippingEnabledCountries = true, ToAllShippingEnabledStatesOrProvinces = true });

         //   modelBuilder.Entity<SiteSettings>().HasSeedData(
         //         new SiteSettings("JeeblySettings") { Module = "ShippingJeebly", IsVisibleInCommonSettingPage = false, Value = "{\"customer_code\":\"HCS\",\"service_type_id\":\"PREMIUM\",\"load_type\":\"NON-DOCUMENT\",\"is_risk_surcharge_applicable\":\"True\",\"ApiURl\":\"http://demodashboardapi.shipsy.in/api/customer/integration/consignment/softdata\",\"ApiKey\":\"47ef0ed0b30b600e4335371ee1370f\"}", SiteId = 1L }

         //);
        }
    }
}

