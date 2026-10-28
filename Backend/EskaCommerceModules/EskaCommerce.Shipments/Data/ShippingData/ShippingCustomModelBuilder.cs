using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Shipping;

namespace EskaCommerce.Shipping.Data
{
    public class ShippingCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<ShippingProvider>().ToTable("Shipping_ShippingProvider");
        }
    }
}
