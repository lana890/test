using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class AdditionalSettingsVM
    {
        public long CartItem { get; set; }
        public List<string> AdditionalSettings { get; set; }
    }
}
