using System.Collections.Generic;
using EskaCMS.Entities.Entity.Pricing;

namespace EskaCommerce.Pricing.Services
{
    public class CouponValidationResult
    {
        public bool Succeeded { get; set; }
        public bool IsAutoCoupon {  get; set; }
        public decimal TotalAmmountAfterDiscount { get; set; }

        public decimal DiscountAmount { get; set; }
		public string DiscountType { get; set; }
		public string DiscountValue { get; set; }


		public string CouponCode { get; set; }

        public string CouponRuleName { get; set; }

        public string ErrorMessage { get; set; }

        public long CouponId { get; set; }

        public CartRule CartRule { get; set; }
        public bool IsFreeShipping {  get; set; }

        public System.Collections.Generic.IList<DiscountedProduct> DiscountedProducts { get; set; } = new System.Collections.Generic.List<DiscountedProduct>();
    }
}
