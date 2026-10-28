using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public class CheckoutSessionVM
    {

        public string CheckoutJsUrl { get; set; }
        // CheckoutJsUrl = $@"{GatewayApiConfig.GatewayUrl}/checkout/version/{GatewayApiConfig.Version}/checkout.js";
        public string MerchantId { get; set; }

        public long OrderId { get; set; }
        public string  OrderCode { get; set; }
        public CheckoutSessionModel CheckoutSession { get; set; }
        public string Currency { get; set; }


    }

}
