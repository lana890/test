using System;
using System.Collections.Generic;
using EskaCMS.Entities.Entity.Shipments;
using EskaCommerce.Shipments.Models;
using EskaCommerce.Shipments.Services.Shippment;

namespace EskaCommerce.Shipments.Areas.Shipments.ViewModels
{
    public class ShipmentForm
    {
        public long OrderId { get; set; }

        public long WarehouseId { get; set; }

        public string TrackingNumber { get; set; }

        public IList<ShipmentItemVm> Items { get; set; } = new List<ShipmentItemVm>();
    }

    public class ShipmentAdminViewModel
    {
        public long OrderId { get; set; }

        public string City { get; set; }

        public ShippmentStatus ShipmentStatus  { get; set; }
        public string TrackingNumber { get; set; }
        public long Id { get; set; }

        public IList<ShipmentItemViewModel> Items { get; set; } = new List<ShipmentItemViewModel>();
    }

    public class ShipmentItemViewModel
    {
        public string ProductName { get; set; }

        public long Quentity { get; set; }

    }

    public class ShipmentHistoryViewModel
    {
        public long ShipmentId { get; set; }


        public string? OldStatus { get; set; }

        public string NewStatus { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public string UserName { get; set; }


    }
    public class DeliveryStatusViewModel
    {
        public int Cancelled { get; set; }
        public int Pending { get; set; }
        public int Ready { get; set; }
        public int Delivered { get; set; }
        public long ShipmentId { get; set; }
        public long OrderId { get; set; }
        public string TrackingNumber { get; set; }
        public string Warehouse { get; set; }

    }

}
