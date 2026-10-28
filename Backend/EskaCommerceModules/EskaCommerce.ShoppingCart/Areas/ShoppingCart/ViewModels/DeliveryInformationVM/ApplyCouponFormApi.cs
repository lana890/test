using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels.DeliveryInformationVM
{
    public class ApplyCouponFormApi
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        public string CouponCode { get; set; }
        public CartTypes CartType { get; set; } = CartTypes.Shop;
        public long ProductId { get; set; }
        public int Quantity { get; set; } = 1;

        public string AdditionalDetails { get; set; }
    }
}
