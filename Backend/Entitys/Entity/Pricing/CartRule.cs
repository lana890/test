using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EskaCMS.Infrastructure.Models;
using EskaCommerce.Infrastructure.Models;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Pricing
{
    public class CartRule : EntityBase
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset? StartOn { get; set; }

        public DateTimeOffset? EndOn { get; set; }

        public bool IsCouponRequired { get; set; }

        [StringLength(450)]
        public string RuleToApply { get; set; }

        public decimal DiscountAmount { get; set; }
        public EStatus Status { get; set; }
        public decimal? MaxDiscountAmount { get; set; }

        public int? DiscountStep { get; set; }

        public int? UsageLimitPerCoupon { get; set; }

        public int? UsageLimitPerCustomer { get; set; }

        //  public bool IsAutoDiscount { get; set; }
        //public bool IsValidateQuantity { get; set; }
        public bool IsAutoCouponBasedOnQnt { get; set; }
        public bool IsAutoDiscount { get; set; }
        public int? Quantity { get; set; }
        public long SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public bool IsCustomerGroup { get; set; }

        public IList<Coupon> Coupons { get; set; } = new List<Coupon>();

        public IList<CartRuleCustomerGroup> CustomerGroups { get; set; } = new List<CartRuleCustomerGroup>();

        public IList<CartRuleProduct> Products { get; set; } = new List<CartRuleProduct>();

        public IList<CartRuleCategory> Categories { get; set; } = new List<CartRuleCategory>();
    }
}
