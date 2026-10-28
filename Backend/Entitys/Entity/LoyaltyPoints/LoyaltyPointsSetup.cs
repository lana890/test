using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.LoyaltyPoints.Enums.LoyaltyPointsEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.LoyaltyPoints
{
    public class LoyaltyPointsSetup : EntityBase
    {

        public decimal FromPrice { get; set; }
        public decimal ToPrice { get; set; }
        public decimal PointPerUnit { get; set; }
        public decimal PointAmount { get; set; }
        public EStatus Status { get; set; }
        public ELoyalitySource LoyalitySource { get; set; }

        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }
        public long? ModifiedById { get; set;}
        public User ModifiedBy { get; set; }

        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; } 
        public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset? ModificationDate { get; set; }
        public long SiteId { get; set; }

        public Site Site { get; set; }

        public long? ProductId { get; set; }

        public Product Product { get; set; }

        public List<LoyaltyPointsSetupHistory> LoyaltySetupHistory { get; set; }

    }
}
