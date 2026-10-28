#nullable enable
using Org.BouncyCastle.Bcpg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Models.PayFort
{
    public class ConfigurePayFort
    {

        public string URl { get; set; }
        public string ReturnUrl { get; set; }
        public string MerchantIdentifier { get; set; }
        public string RateJordan { get; set; }
        public string RateUAE { get; set; }
        public string service_command { get; set; }
        public string Rate { get; set; }
        public string Command { get; set; }
        public string AccessCode { get; set; }
        public string RequestPharse { get; set; }
        public string ResponsePhrase { get; set; }

        public string BaseUrlForEnglish { get; set; }
        public string BaseUrlForArabic { get; set; }
        public string device_Id { get; set; }
        public string payfortUrl { get; set; }
        public string MobileSDKtokenUrl { get; set; }

        public bool? enableEmail { get; set; } = true;
        public string NotificationUrl { get; set; }
        public string CP_URL { get; set; }
        public string AllowedCurrencies {  get; set; }
        public string CartDesc { get; set; }

        public string Environment { get; set; }
    }
}
