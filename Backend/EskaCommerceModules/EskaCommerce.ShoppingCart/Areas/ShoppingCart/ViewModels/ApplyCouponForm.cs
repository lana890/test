using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels
{
    public class ApplyCouponForm
    {
        public string CouponCode { get; set; }
        public CartTypes CartType { get; set; }
    }



    public class ApplyCouponForOrderVM
    {
        public string CouponCode { get; set; }
        public long OrderId { get; set; }
    }


}
