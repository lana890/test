using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Payments.Models.PaymentHyperPay
{
    public class HyperpayRequestViewModel
    {
        public string id { get; set; }
        public string url { get; set; }
        public string redirectUrl { get; set; }
    }


    public class CreateHyperPaySessionVM
    {
        public HyperPayEntityType EntityType { get; set; }
        public string FrontReturnURl { get; set; }


    }

}
