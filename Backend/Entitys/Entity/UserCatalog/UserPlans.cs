
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.UserCatalog
{
    public  class UserPlans :EntityBase
    {

        public UserPlans()
        {
            ActivationDate = DateTimeOffset.Now;
        }
        public User User { get; set; }
        public long  UserId { get; set; }
        public PlansVersions PlanVersions { get; set; }
        public long PlanVersionsId { get; set; }
        public string  AgreementId { get; set; }

        public string Tran_ref { get; set; }

        public Order Order { get; set; }
        public long OrderId { get; set; }
        public Enum.GeneralEnums.EUserPlansStatus CurrentStatus { get; set; }
        public DateTimeOffset CurrentStatusDate { get; set; }
        public DateTimeOffset ActivationDate { get; set; }

        public Enum.GeneralEnums.EUserPlansStatus NextStatus { get; set; }
        public DateTimeOffset NextStatusDate { get; set; }

        [System.ComponentModel.DataAnnotations.StringLength(450)]
        public string Reason { get; set; }
        public List<UserPlansHistory>  userPlansHistories { get; set; }

        public UserPlans Parent { get; set; }
        public long? ParentId { get; set; }
        public bool IsSubscribed { get; set; }
    }
}
