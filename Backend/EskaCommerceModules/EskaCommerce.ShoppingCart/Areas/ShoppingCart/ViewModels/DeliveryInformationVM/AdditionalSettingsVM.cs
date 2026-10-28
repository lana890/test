using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels.DeliveryInformationVM
{
    public class AdditionalSettingsVM
    {
        public long CartItem { get; set; }
        public List<string> AdditionalSettings { get; set; }
    }
}
