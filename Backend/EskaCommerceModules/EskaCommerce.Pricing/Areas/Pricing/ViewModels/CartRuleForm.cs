using EskaCMS.Entities.Entity.Core;
using EskaCommerce.Pricing.Areas.Pricing.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EskaCommerce.Pricing.Areas.Pricing.ViewModels
{
    public class CartRuleForm
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset? StartOn { get; set; }

        public DateTimeOffset? EndOn { get; set; }

        public bool IsCouponRequired { get; set; }

        public string CouponCode { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string RuleToApply { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public decimal DiscountAmount { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        public int? DiscountStep { get; set; }

        public int? UsageLimitPerCoupon { get; set; }

        public int? UsageLimitPerCustomer { get; set; }
        public int? Quantity { get; set; }
        public IList<CartRuleProductVm> Products { get; set; } = new List<CartRuleProductVm>();
        public IList<CustomerGroupsForm> CustomerGroups { get; set; } = new List<CustomerGroupsForm>();

        public bool IsCustomerGroup { get; set; }
        public bool IsAutoDiscount { get;  set; }
    }
    public class CustomerGroupsForm
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }

    }

    public class CartCustomerRuleForm
    {
        public long CartId { get; set; }

        public IList<long> CustomerGroupIds = new List<long>();

    }

    public class CartPlanRuleForm
    {
        public long CartId { get; set; }

        public long PlanVersionId { get; set; }

    }
    public class UpsertCartRuleGroups
    {
        public long CartId { get; set; }

        public IList<long> CustomerGroupId = new List<long>();

    }


    public class GetRuleCustomerGroupViewModel
    {


        public IList<DetailsRuleCustomerGroupViewModel> CustomerGroup { get; set; } = new List<DetailsRuleCustomerGroupViewModel>();

        public long CartRuleId { get; set; }

    }

    public class DetailsRuleCustomerGroupViewModel
    {
        public string Name { get; set; }
        public long Id { get; set; }


    }
}
