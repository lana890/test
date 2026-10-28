using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public class MepsConfig
    {
        public string Module { get; set; }


        public string MerchantId { get; set; } 
        public string ApiPassword { get; set; } 
        public string BaseUrl { get; set; }
        public string Currency { get; set; }
        public string Version { get; set; } 
        public string Enviroment { get; set; }
        public string ServerKey { get; set; } 
        public string ReturnUrl { get; set; } 
        public string AllowedCurrencies { get; set; } 

    }
}
