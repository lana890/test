using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductBranch : EntityBase
    {
        public long ProductId { get; set; }
        public Product Product { get; set; }

        public long WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

    }
}
