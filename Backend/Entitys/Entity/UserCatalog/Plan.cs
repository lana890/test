
using EskaCMS.Infrastructure.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.UserCatalog
{
   public class Plan : EntityBase
    {
        public bool IsFeatured { get; set; }
        public bool IsTopPicked { get; set; }
        public string Image { get; set; }
        public long SiteId { get; set; }
        public PlanTypes PlanType { get; set; }
        public RecurringTypes RecurringType { get; set; }
       
        public EStatus Status { get; set; }
        public User CreatedBy { get; set; }
        public bool IsCustomPlan { get; set; }
        public long CreatedById { get; set; }
        public int Order { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public IList<PlanProductVersion> PlanProducts { get; set; }
        public IList<PlansVersions> Versions { get; set; }
        public IList<PlanDetails> PlanDetails { get; set; }
        public IList<PlanCategory> Categories { get; set; }

        public long? NextPlanId { get; set; }
        public Plan NextPlan{ get; set; }

    }

}
