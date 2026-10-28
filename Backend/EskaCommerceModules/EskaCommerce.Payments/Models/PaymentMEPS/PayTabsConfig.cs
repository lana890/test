using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public class PayTabsConfig
    {
        public string Environment { get; set; }
        public string PayTapsURL { get; set; }
        public string ServerKey { get; set; }
        public string MerchantCountryCode { get; set; }
        public string CallBackURL { get; set; }
        public string ReturnURL { get; set; }
        public string ReturnURLEN { get; set; }
        public string ReturnURLAR { get; set; }
        public string ProfileId { get; set; }
        public string CartDesc { get; set; }
        public string AllowedCurrencies { get; set; }
        public string ClientKey  { get; set; }
        public string Tokenize  { get; set; }
        public bool? enableEmail { get; set; } = true;
        public string CP_URL { get; set; } 
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

    public class PaymentConfigurationMobileViewModel
    {
        public PaymentConfigurationMobileViewModel()
        {
            BillingDetails = new BillingDetailsViewModel();
            // payment_result = new Payment_Result();
            // payment_info = new Payment_Info();
        }
        internal string languageCode;

        public int ProfileID { get; set; }

      
        public string ServerKey { get; set; }

      
        public string ClientKey { get; set; }

        public string CartID { get; set; }

      
        public string Currency { get; set; }

        public string CartDescription { get; set; }

        public string MerchantCountryCode { get; set; }

     
        public string MerchantName { get; set; }

        public float Amount { get; set; }

     
        public string ScreenTitle { get; set; }

        
        public dynamic BillingDetails { get; set; }

        public bool ForceShippingInfo { get; set; }
        public string EncryptedOrderId { get; set; }
    }

    public class RequestPayTabsViewModel
    {
        public CartTypes CartType { get; set; }
        public string PaymentProviderId { get; set; }
        public string ReturnUrl { get; set; }


    }
}
