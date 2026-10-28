using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.LoyaltyPoints.Enums.LoyaltyPointsEnums;

namespace EskaCMS.Entities.Entity.LoyaltyPoints
{
    public class LoyaltyWallet : EntityBase
    {
        public ELoyalityWalletStatus Status { get; set; }
        public List<LoyaltyWalletTransactions> LoyaltyWalletTransactions { get; set; }
        public long UserId { get; set; }
        public User User { get; set; }
        public long SiteId { get; set; }
        public Site Site { get; set; }
    }
}
