using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels
{
    public class CartQuantityUpdate
    {
        public long CartItemId { get; set; }

        public int Quantity { get; set; }
        public CartTypes CartType { get; set; }
    }
}
