using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Entities.Entity.ShoppingCart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Payments.Models.TransactionHistory
{
    public class TransactionHistoryDto
    {
        public long? OrderId { get; set; }
        public long? CartId { get; set; }
        public long PaymentId { get; set; }
        public string PaymentMethod { get; set; }


        public PaymentStatus? OldStatus { get; set; }
        public PaymentStatus NewStatus { get; set; }
        public string Info { get; set; }
        public DateTimeOffset Date { get; set; }
        public long UserId { get; set; }

        public EPlatformSource Channel { get; set; }

        public long? ParentId { get; set; }
    }
}
