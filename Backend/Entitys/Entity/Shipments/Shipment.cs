using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Infrastructure.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Enum;

namespace EskaCMS.Entities.Entity.Shipments
{
    public class Shipment : EntityBase
    {
        public Shipment()
        {
            CreatedOn = DateTimeOffset.Now;
            LatestUpdatedOn = DateTimeOffset.Now;
        }


        public long OrderId { get; set; }

        public Order Order { get; set; }

        [StringLength(450)]
        public string TrackingNumber { get; set; }

        public string LabelUrl { get; set; }

        public long? WarehouseId { get; set; }

        public long? VendorId { get; set; }

        public Warehouse Warehouse { get; set; }

        public long CreatedById { get; set; }

        public User CreatedBy { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }

        public ShippmentStatus ShippmentStatus { get; set; }

        public DeliveryOptionEnum ShippingDelivery { get; set; }
        
        public long? PickupId { get; set; } 
        public Pickup Pickup { get; set; } 
        public IList<ShipmentItem> Items { get; set; } = new List<ShipmentItem>();
        public IList<ShipmentHistory> ShipmentHistory { get; set; } = new List<ShipmentHistory>();
    }
}
