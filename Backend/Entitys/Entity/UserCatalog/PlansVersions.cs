using EskaCMS.Entities.Entity.Tax;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.PlanEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pricing;

namespace EskaCMS.Entities.Entity.UserCatalog
{
    public  class PlansVersions : EntityBase
    {

        public Plan Plan { get; set; }
        public long PlanId { get; set; }
        public int VersionId { get; set; }
        public int? RepeatAmount { get; set; }
        public RepeatPeriod RepeatPeriod { get; set; }
        public int RepeatInterval { get; set; }
        public int RepeatStart { get; set; }
        public int? RepeatTerm { get; set; }
        public int? RepeatFinal { get; set; }
        public User  CreatedBy { get; set; }
        public long CreatedById { get; set; }
        public long? DiscountId { get; set; }
        public EskaCMS.Entities.Entity.Discount.Discount Discount { get; set; }


        public long? CartRuleId { get; set; }
        public CartRule CartRule { get; set; }
        public long? TaxClassId { get; set; }

        public TaxClass TaxClass { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public IList<PlanProductVersion> PlanProductVersions { get; set; }
        public IList<UserPlans> UserPlans { get; set; }
        public int RefundPeriod { get; set; }

    }
}
