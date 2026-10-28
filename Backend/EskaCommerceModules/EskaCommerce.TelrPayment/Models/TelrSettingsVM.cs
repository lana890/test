using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.TelrPayment.Models
{
   public class TelrSettingsVM
    {
       
        public string BaseAddress { get; set; }
        public string URLType { get; set; }
        public string Authkey { get; set; }
        public string StoreId { get; set; }
        public string return_auth { get; set; }
        public string return_can { get; set; }
        public string return_decl { get; set; }
        public string DataIntegritySecretkey { get; set; }

        public string TestMode { get; set; } /// 1==> Test 0 ==> Live

    }
}
