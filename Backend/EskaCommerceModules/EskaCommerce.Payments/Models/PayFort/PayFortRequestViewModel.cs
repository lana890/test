#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Models.PayFort
{
    public class PayFortRequestViewModel
    {
        public string Command { get; set; }
        public string RequestPhrase { get; set; }

        public string AccessCode { get; set; }

        public string Merchant_Identifier { get; set; }

        public string Merchant_Reference { get; set; }

        public string merchant_extra { get; set; }
        public string merchant_extra1 { get; set; }
        public string merchant_extra2 { get; set; }
        public string merchant_extra3 { get; set; }
        public string merchant_extra4 { get; set; }

        public string Amount { get; set; }


        public string Currency { get; set; }

        public string Language { get; set; }


        public string CustomerEmail { get; set; }
        public string CustomerName { get; set; }



        public string Signature { get; set; }

        public string order_description{ get; set; }
        public string phone_number { get; set; }
        public string customer_ip { get; set; }


        public string ReturnUrl { get; set; }
        public string Url { get; set; }

        public string Rate { get; set; }
        public string sdk_token {  get; set; }
        public string OrderId {  get; set; }
        public string notification_url {  get; set; }
    }
}
