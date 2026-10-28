using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.TelrPayment.Models
{
  public  class TelrResponseVM
    {
        // public long UserAddressId { get; set; }
        public string method { get; set; }
        public string trace { get; set; }
        public SuccessOrder order { get; set; }
        public Error error { get; set; }

       

    }
    public  class SuccessOrder
    {
        public string @ref { get; set; }
        public string url { get; set; }
        public long id { get; set; }
    }
    public class Error
    {
        public string message { get; set; }
        public string note { get; set; }
        public string details { get; set; }
    }

}

