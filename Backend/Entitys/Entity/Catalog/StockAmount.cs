using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class StockAmount : EntityBase
    {
        public long ProductId { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public long WareHouseId { get; set; }
        public Warehouse WareHouse { get; set; }

        public DateTimeOffset LastUpdated { get; set; }
    }
}
