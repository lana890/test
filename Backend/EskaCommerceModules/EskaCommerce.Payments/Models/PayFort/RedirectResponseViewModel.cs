#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Models.PayFort
{
    public class RedirectResponseViewModel
    {
        public string PayfortInfo { get; set; }

        public string Url { get; set; }
    }
}
