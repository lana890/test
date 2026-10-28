using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public class ViewDataVM
    {
        public string Operation { get; set; }

        public string Method { get; set; }
        public string RequestUrl { get; set; }

        public string Payload { get; set; }
        public string Response { get; set; }


    }
}
