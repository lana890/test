using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Aramex.Models.Requests.Cart
{
    public class AdditionalSettingsDtos
    {
        public long CartItem { get; set; }
        public List<string> AdditionalSettings { get; set; }
    }
}
