
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using Microsoft.EntityFrameworkCore;

namespace EskaCommerce.Inventory.Data
{
    public class InventoryCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Warehouse>().HasSeedData(new Warehouse(1) { Name = "Default warehouse", AddressId = 1 });
        }
    }
}

