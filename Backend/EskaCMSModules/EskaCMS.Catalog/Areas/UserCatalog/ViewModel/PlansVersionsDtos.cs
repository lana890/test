
using EskaCMS.Entities.Entity.Tax;
using System;
using System.Collections.Generic;

namespace EskaCommerce.UserCatalog.Dtos
{
    public class PlansVersionsCreate
    {
        public long PlanId { get; set; }
        public int? RepeatAmount { get; set; }
        public string RepeatPeriod { get; set; }
        public int? RepeatInterval { get; set; }
        public int RepeatStart { get; set; }
        public int? RepeatTerm { get; set; }
        public int? RepeatFinal { get; set; }
        public long? DiscountId { get; set; }
        public long? CartRuleId { get; set; }
        public long? TaxClassId { get; set; }
        public int RefundPeriod { get; set; }

    }

    public class PlansVersionsListInput
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
    public class PlansVersionsOutput
    {
        public long Id { get; set; }
        public PlanOutput Plan { get; set; }
        public long PlanId { get; set; }
        public int VersionId { get; set; }
        public int? RepeatAmount { get; set; }
        public EskaCMS.Entities.Enum.PlanEnums.RepeatPeriod RepeatPeriod { get; set; }
        public int RepeatInterval { get; set; }
        public int RepeatStart { get; set; }
        public int? RepeatTerm { get; set; }
        public int? RepeatFinal { get; set; }
        public long CreatedById { get; set; }
        public IList<PlanProductVersionOutput> PlanProducts { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? DiscountId { get; set; }
        public long? CartRuleId { get; set; }
        public decimal DiscountValue { get; set; }
        public long? TaxClassId { get; set; }
        public long? PriceTotal { get; set; }

        public int RefundPeriod { get; set; }

    }

    public class PlansVersionsVM
    {
        public long Id { get; set; }
        public int VersionId { get; set; }
        public int? RepeatAmount { get; set; }
        public EskaCMS.Entities.Enum.PlanEnums.RepeatPeriod RepeatPeriod { get; set; }
        public int RepeatInterval { get; set; }
        public int RepeatStart { get; set; }
        public int? RepeatTerm { get; set; }
        public int? RepeatFinal { get; set; }
        public IList<PlanProductVersionVM> PlanProducts { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? DiscountId { get; set; }
        public long? CartRuleId { get; set; }
        public long? TaxClassId { get; set; }
        public int RefundPeriod { get; set; }

    }
}
