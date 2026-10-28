using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.LoyaltyPoints.Enums.LoyaltyPointsEnums;

namespace EskaCMS.Entities.Entity.LoyaltyPoints
{
    public class LoyaltyWalletTransactions : EntityBase
    {
        public decimal PointsAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public EWalletOperationType OperationType { get; set; }
        public string Note { get; set; }
        public long? LoyaltyPointsSetupId { get; set; }
        public LoyaltyPointsSetup LoyaltyPointsSetup { get; set; }
        public long LoyaltyWalletId { get; set; }
        public LoyaltyWallet LoyaltyWallet { get; set; }
        public long? OrderId { get; set; }
        public Order Order { get; set; }

        public long? CartId { get; set; }
        public Cart Cart { get; set; }
        public long? CreatedById { get; set; }
        public User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.Now;
    }
}
