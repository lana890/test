using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Orders;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
   public  class UpdateAddressVM
    {
        public OrderAddress ShippingAddress { get; set; }
        public OrderAddress BillingAddress { get; set; }

        public bool UseShippingAsBillingAddress { get; set; }
        public bool IsUpdateAdditionalAddresses { get; set; }
        public bool IsUpdateOrderAddress { get; set; }
        public string AdditionalDetails { get; set; }

        public long? ItemId { get; set; }



    }
}
