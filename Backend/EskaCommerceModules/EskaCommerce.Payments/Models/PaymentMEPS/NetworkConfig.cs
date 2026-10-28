using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public class NetworkConfig
    {
        public string VERSION { get; set; }
        public string PASSWORD { get; set; }
        public string BASE_URL { get; set; }
        public string CURRENCY { get; set; }
        public string CURRENCY_CULTUER { get; set; }
        public string MASTER_API_PASSWORD { get; set; }
        public string MERCHANT_ID { get; set; }
        public string MERCHANT_NAME { get; set; }
        public string RETURN_URL { get; set; }
        public string AllowedCurrencies { get; set; }

    }
}
