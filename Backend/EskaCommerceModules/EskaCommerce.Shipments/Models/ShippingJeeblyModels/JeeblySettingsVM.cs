using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.ShippingJeebly.Models
{
    public class JeeblySettingsVM
    {

        public string customer_code { get; set; }
        public string service_type_id { get; set; }
        public string load_type { get; set; }
        public bool is_risk_surcharge_applicable { get; set; }
        public string ApiURl { get; set; }
        public string ApiKey { get; set; }
    
      
             
          
    }
}
