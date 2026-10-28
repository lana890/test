using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace EskaCommerce.TelrPayment.Models
{

  

    public class RefundResponseVM
    {
    
        public remote remote { get; set; }
    }

   
    public class remote
    {
        public AuthRemote auth { get; set; }
        public PaymentRemoteVM payment { get; set; }
    }

    public class AuthRemote
    {
        public string status { get; set; }
        public string code { get; set; }
        public string message { get; set; }
        public string tranref { get; set; }
        public string cvv { get; set; }
        public string avs { get; set; }
        public string trace { get; set; }
    }
    public class PaymentRemoteVM
    {

        public string code { get; set; }
        public string description { get; set; }
        public string card_end { get; set; }
        public string card_bin { get; set; }
    }

}
