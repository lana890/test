using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public class IPNCallBack
    {
        public int merchant_id { get; set; }
        public int profile_id { get; set; }

        public string tran_ref { get; set; }
        public string tran_type { get; set; }

        public string cart_id { get; set; }
        public float cart_amount { get; set; }
        public string cart_currency { get; set; }
        public string cart_description { get; set; }

        public string tran_class { get; set; }
        public string tran_currency { get; set; }
        public float tran_total { get; set; }
        public string token { get; set; }

        public CustomerDetailsResult customer_details { get; set; }

        public PaymentResult payment_result { get; set; }

        public PaymentInfo payment_info { get; set; }


        public long GetCulture()
        {
            var parts = cart_description?.Split('&');
            return (parts?.Length > 2 && long.TryParse(parts[2], out var result)) ? result : 0;
        }

        public long GetSiteId()
        {
            var parts = cart_description?.Split('&');
            return (parts?.Length > 1 && long.TryParse(parts[1], out var result)) ? result : 0;
        }

        public long GetWarehouse()
        {
            var parts = cart_description?.Split('&');
            return (parts?.Length > 3 && long.TryParse(parts[3], out var result)) ? result : 0;
        }

        public long GetOrderId()
        {
            var parts = cart_id?.Split('-');
            return (parts?.Length > 1 && long.TryParse(parts[1], out var result)) ? result : 0;
        }
        public string GetProvider()
        {
            var parts = cart_description?.Split('&');
            return (parts?.Length > 4 && parts[4]!=null) ? parts[4].Trim() : null;
        }
        //CP
        public string GeStep()
        {
            var parts = cart_description?.Split('&');
            return (parts?.Length > 5 && parts[5] != null) ? parts[5].Trim() : null;
        }

        public string GetSessionId()
        {
            var parts = cart_description?.Split('&');
            return (parts?.Length > 6 && parts[6]!=null) ? parts[6].Trim() : null;
        }
    }

  
    public class CustomerDetailsResult
    {

        public string name { get; set; }

        public string email { get; set; }
        public string phone { get; set; }

        public string country { get; set; }
        public string state { get; set; }
        public string city { get; set; }
        public string street1 { get; set; }
        public string ip { get; set; }





    }
    public class PaymentResult
    {
        public string response_status { get; set; }
        public string response_code { get; set; }
        public string response_message { get; set; }
        public string acquirer_message { get; set; }
        public string acquirer_rrn { get; set; }
        public string cvv_result { get; set; }
        public string avs_result { get; set; }
        public DateTimeOffset transaction_time { get; set; }


    }

    public class PaymentInfo
    {
        public string card_type { get; set; }
        public string card_scheme { get; set; }
        public string payment_description { get; set; }
    }
}
