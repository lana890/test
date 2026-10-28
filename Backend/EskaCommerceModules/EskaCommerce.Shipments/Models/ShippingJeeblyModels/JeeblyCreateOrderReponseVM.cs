using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.ShippingJeebly.Models
{
    public class JeeblyCreateOrderReponseVM
    {


        public string status { get; set; }
        public List<Data> data { get; set; }


    }
    public class Data
    {
        public bool success { get; set; }
        public string reference_number { get; set; }
        public string barCodeData { get; set; }
        public string customer_reference_number { get; set; }
        public List<Pieces> pieces { get; set; }
        public string reason { get; set; }
        public string message { get; set; }
        public bool should_retry { get; set; }


    }
    public class Pieces
    {
        public string reference_number { get; set; }
    }
}
