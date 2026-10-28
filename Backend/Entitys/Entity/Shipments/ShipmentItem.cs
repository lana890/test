
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Orders;

namespace EskaCMS.Entities.Entity.Shipments
{
    public class ShipmentItem : EntityBase
    {
        public long ShipmentId { get; set; }

        public Shipment Shipment { get; set; }

        public long OrderItemId { get; set; }
        public OrderItem OrderItem { get; set; }

        public long ProductId { get; set; }

        public Product Product { get; set; }

        public int Quantity { get; set; }
    }
}
