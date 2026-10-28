using EskaCMS.Core.Services;
using EskaCMS.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class OrderHistoryListItem
    {
        private readonly ICurrencyService _currencyService;

        public OrderHistoryListItem(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public long Id { get; set; }
        public long  ShipmentId { get; set; }
        public DeliveryOptionEnum ShippingDelivery { get; set; }
        public string TrackingNumber { get; set; }
        public long? PickupId { get; set; }
        public DateTimeOffset CreatedOn { get; set; }

        public decimal SubTotal { get; set; }

        public string SubTotalString { get { return _currencyService.FormatByUserCurrency(SubTotal); } }

        public string OrderStatus { get; set; }
        public string EncryptedOrderId { get; set; }

        public IList<OrderHistoryProductVm> OrderItems { get; set; } = new List<OrderHistoryProductVm>();
        public IList<OrderAdditionalFeesVM> OrderAdditionalFees { get; set; } = new List<OrderAdditionalFeesVM>();
        public int? EstimatedDate { get;  set; }
    }

   
}
