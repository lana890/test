using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels
{
    public class SaveOrderNote
    {
        public string OrderNote { get; set; }
        public CartTypes CartType { get; set; }
    }
}
