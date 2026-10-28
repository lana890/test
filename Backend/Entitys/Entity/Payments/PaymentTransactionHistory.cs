using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Entity.TelrPayment;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Payments
{
    public class PaymentTransactionHistory: EntityBase
    {
        public long? OrderId { get; set; }
        public Order Order { get; set; }
        public long? CartId { get; set; }
        public Cart Cart { get; set; }
        public long PaymentId { get; set; }
        public Payment Payment { get; set; }
        public string PaymentMethod { get; set; }


        public PaymentStatus? OldStatus { get; set; }
        public PaymentStatus NewStatus { get; set; }
        public string Info { get; set; }
        public DateTimeOffset Date { get; set; }
        public long UserId { get; set; }
        public User User { get; set; }

        public EPlatformSource Channel { get; set; }

        public long? ParentId { get; set; }
        public PaymentTransactionHistory Parent { get; set; }
        // For Plan (Recuring)
        public string AgreementId { get; set; }


    }
}
