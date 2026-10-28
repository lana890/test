using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Entities.Entity.UserCatalog
{
    public class UserPlansHistory : EntityBase
    {

        public UserPlans UserPlan { get; set; }
        public long UserPlanId { get; set; }
        public Enum.GeneralEnums.EUserPlansStatus CurrentStatus { get; set; }
        public Enum.GeneralEnums.EUserPlansStatus? OldStatus { get; set; }
     
        public DateTimeOffset TransDate { get; set; }
        public string AdditionalInfo { get; set; }
    }
}
