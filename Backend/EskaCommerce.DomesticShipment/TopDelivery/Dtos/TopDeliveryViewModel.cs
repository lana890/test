using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.DomesticShipment.TopDelivery.Dtos
{
    public class SettingsTopDeliveryViewModel
    {
        public string BaseUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string BaseUrlStatus { get; set; }
        public string BaseUrlEstimateDeliveryPrice { get; set; }
    }
    public class TopDeliveryOrderViewModel
    {
        public string Username { get; set; } // Required
        public string Password { get; set; } // Required
        public string BranchId { get; set; } // Required
        public string DeliveryAddress { get; set; } // Required
        public string CustomerPhone { get; set; } // Optional
        public string CustomerEmail { get; set; } // Optional
        public string CustomerName { get; set; } // Optional
    }

    public class EstimateDeliveryPriceResponse
    {
        [JsonProperty("deliveryPrice")]
        public string DeliveryPrice { get; set; }

        [JsonProperty("deliveryTime")]
        public string DeliveryTime { get; set; }
    }
    public class  TopDeliveryOrderResponseViewModel
    {
        public string Result { get; set; }
        public int OrderNumId { get; set; }
        public string orderId { get; set; }
    }
    public class DeliveryStatusViewModel
    {
        public int Placed { get; set; }
        public int ToRestaurant { get; set; }
        public int ToCustomer { get; set; }
        public int Delivered { get; set; }
        public int Canceled { get; set; }
        public long ShipmentId { get; set; }
        public long OrderId { get; set; }
        public string TrackingNumber { get; set; }
        public int? EstimatedTime { get; set; }

      
    }


}
