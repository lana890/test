using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductPriceWarehouse :EntityBase
    {
        public long ProductId { get; set; }
        public Product Product { get; set; }
        public long WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }
        public decimal Price { get; set; }
        public decimal? ForeignPrice { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public long CreatedById { get; set; }
        public User CreatedBy{ get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public long? ModifiedById { get; set; }
        public User ModifiedBy { get; set; }
    }
}