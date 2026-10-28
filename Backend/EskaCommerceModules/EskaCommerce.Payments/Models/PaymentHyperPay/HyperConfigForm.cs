using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Payments.Models.PaymentHyperPay
{
    public class HyperConfigForm
    {
        public List<EntitiesTypes> EntitiesTypes { get; set; }
        public string token { get; set; }
        public string url { get; set; }
        public string ReturnPath { get; set; }
        public string BaseURL { get; set; }
        public string Environment { get; set; }
        public string SuccessResponseCode { get; set; }

    }
    public class EntitiesTypes
    {
        public string EntityId { get; set; }
        public string EntityType { get; set; }
    }
}
