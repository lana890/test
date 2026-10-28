using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCommerce.UserCatalog.Dtos
{
  public  class UserPlansHistoryVM
    {
        public UserPlans UserPlan { get; set; }
        public long UserPlanId { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EUserPlansStatus CurrentStatus { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EUserPlansStatus? OldStatus { get; set; }

        public DateTimeOffset TransDate { get; set; }
        public string AdditionalInfo { get; set; }
    }
}
