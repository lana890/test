#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Payments.Models.PayFort
{
    public class PayFortMobilPayload
    {
        public CartTypes CartType { get; set; }
        //public string sdk_token { get; set; }
        public string device_id { get; set; }
        public int Type { get; set; }
		public long? CartId { get; set; }

	}

	public class PayFortMobileRequest
    {

      
            public PayFortMobileRequest()
            {
                BillingDetails = new BillingDetailsViewModel();
                // payment_result = new Payment_Result();
                // payment_info = new Payment_Info();
            }
            public string languageCode { get; set; }
            public string fort_id {  get; set; }

            public string CustomerId {  get; set; }


            public string CartID { get; set; }

       public string CartDesc {  get; set; }
            public string Currency { get; set; }

            public string CartDescription { get; set; }

            public string customer_email {  get; set; }
            public string sdk_token { get; set; }
            public string token_name { get; set; }
            public string payment_option {  get; set; }
            public string order_desription {  get; set; }
            public string merchant_reference { get; set; }

            public float Amount { get; set; }

            public dynamic BillingDetails { get; set; }

            }
        

        public class BillingDetailsViewModel
        {

            public string Name { get; set; }


            public string Email { get; set; }


            public string Phone { get; set; }


            public string AddressLine { get; set; }

            public string City { get; set; }
            public string State { get; set; }
            public string CountryCode { get; set; }
            public string Zip { get; set; }
        
    
}
}