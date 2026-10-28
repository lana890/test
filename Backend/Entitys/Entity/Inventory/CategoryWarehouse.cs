using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Inventory
{
    public class CategoryWarehouse :EntityBase
    {
        public long CategoryId { get; set; }
        public Category Category { get; set; }

        public long WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }
    }
}
