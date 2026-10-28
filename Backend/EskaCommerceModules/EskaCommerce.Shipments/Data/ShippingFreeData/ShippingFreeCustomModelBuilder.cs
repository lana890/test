using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity.Shipping;

namespace EskaCommerce.Module.ShippingFree.Data
{
    public class ShippingFreeCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
           //modelBuilder.Entity<ShippingProvider>().HasSeedData(new ShippingProvider("FreeShip") { Name = "Free Ship", IsEnabled = true, ConfigureUrl = "", ShippingPriceServiceTypeName = "EskaCommerce.ShippingFree.Services.FreeShippingServiceProvider,EskaCommerce.ShippingFree", AdditionalSettings = "{MinimumOrderAmount : 1}", ToAllShippingEnabledCountries = true, ToAllShippingEnabledStatesOrProvinces = true });
        }
    }
}

