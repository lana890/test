using EskaCommerce.Shippments.ShippingPrices.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.ShippingPrices.Services
{
    public class GetShippingPriceResponse
    {
        public IList<ShippingPrice> ApplicablePrices = new List<ShippingPrice>();

        public string CarrierName { get; set; }

        public bool IsSuccess { get; set; }

        public string Error { get; set; }
    }
}
