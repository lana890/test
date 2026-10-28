using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.TelrPayment.Models.Refund
{
   public class TelrRefundVM
    {
        public TelrRefundVM()
        {
            remote = new remote();
        }
        public remote remote { get; set; }

    }
    public class remote
    {
        public remote()
        {
            tran = new Trans();
        }
        public string store { get; set; }
        public string key { get; set; }

        public Trans tran { get; set; }
    }
    public class Trans
    {
        public string type { get; set; }
        public string @class { get; set; }
        public string cartid { get; set; }
        public string description { get; set; }
        public string currency { get; set; }
        public string amount { get; set; }
        public string @ref { get; set; }
        public string test { get; set; }
    }

}
