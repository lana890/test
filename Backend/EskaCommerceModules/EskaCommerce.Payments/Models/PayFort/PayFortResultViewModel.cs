#nullable enable
using Google.Apis.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Models.PayFort
{
    public class PayFortResultViewModel
    {
        [JsonProperty("access_code")]
        public string access_code { get; set; }
        [JsonProperty("amount")]
        public string amount { get; set; }
        [JsonProperty("card_holder_name")]
        public string card_holder_name { get; set; }
        [JsonProperty("card_number")]
        public string card_number { get; set; }
        [JsonProperty("command")]
        public string command { get; set; }
        [JsonProperty("currency")]
        public string currency { get; set; }
        [JsonProperty("customer_email")]
        public string customer_email { get; set; }
        [JsonProperty("customer_ip")]
        public string customer_ip { get; set; }
        [JsonProperty("customer_name")]
        public string customer_name { get; set; }
        [JsonProperty("eci")]
        public string eci { get; set; }
        [JsonProperty("order_description")]

        public string order_description { get; set; }
        [JsonProperty("phone_number")]
        public string phone_number { get; set; }

        [JsonProperty("expiry_date")]
        public string expiry_date { get; set; }
        [JsonProperty("fort_id")]
        public string fort_id { get; set; }
        [JsonProperty("language")]
        public string language { get; set; }
        [JsonProperty("merchant_identifier")]
        public string merchant_identifier { get; set; }
        [JsonProperty("merchant_reference")]
        public string merchant_reference { get; set; }

        [JsonProperty("response_code")]
        public string response_code { get; set; }
        [JsonProperty("response_message")]
        public string response_message { get; set; }
        [JsonProperty("signature")]
        public string signature { get; set; }
        [JsonProperty("status")]
        public string status { get; set; }

        [JsonProperty("merchant_extra")]
        public string merchant_extra { get; set; }

        [JsonProperty("merchant_extra1")]

        public string merchant_extra1 { get; set; }

        [JsonProperty("merchant_extra2")]

        public string merchant_extra2 { get; set; }

        [JsonProperty("merchant_extra3")]

        public string merchant_extra3 { get; set; }
        [JsonProperty("merchant_extra4")]

        public string merchant_extra4 { get; set; }
        //[JsonProperty("merchant_extra5")]

        //public string merchant_extra5{ get; set; }
        //[JsonProperty("token_name")]
        //public string token_name { get; set; }

        //public bool IsSuccessfull { get; set; }

        public long GetSiteId()
        {

            return Convert.ToInt64(this.merchant_extra);
        }

        public long GetOrderId()
        {
            return Convert.ToInt64(this.merchant_extra1);
        }
	

		public long GetType()
        {
            return Convert.ToInt64(this.merchant_extra2.Split("culture")[0]);
        }

		

		public string GeStep()
        {
            return this.merchant_extra3;
        }

      

		public string GetCartId()
		{
			return this.merchant_extra4;
		}
		public string GetCulture()
        {
            return this.merchant_extra2.Split("culture")[1];
        }
       
    }
}
