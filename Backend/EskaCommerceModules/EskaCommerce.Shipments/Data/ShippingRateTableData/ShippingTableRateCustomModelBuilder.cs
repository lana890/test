 using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity.Shipping;

namespace EskaCommerce.ShippingTableRate.Data
{
    public class ShippingTableRateCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
        //    modelBuilder.Entity<ShippingProvider>().HasSeedData(
        //              new ShippingProvider("TableRate") { Name = "Table Rate", IsEnabled = true, ConfigureUrl = "shipping-table-rate-config", ShippingPriceServiceTypeName = "EskaCommerce.ShippingTableRate.Services.TableRateShippingServiceProvider,EskaCommerce.ShippingTableRate", AdditionalSettings = null, ToAllShippingEnabledCountries = true, ToAllShippingEnabledStatesOrProvinces = true }
        //);
        }
    }
}

