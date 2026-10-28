

using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Catalog;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Inventory
{
    public class Stock : EntityBase
    {
        public long ProductId { get; set; }

        public Product Product { get; set; }

        public long WarehouseId { get; set; }

        public Warehouse Warehouse { get; set; }

        public int Quantity { get; set; }

        public int ReservedQuantity { get; set; }
        public EStatus Status { get; set; }
    }
}
