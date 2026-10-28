using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Entities.Entity.EcomLog
{
    public class EcommerceLogs : EntityBase
    {
        public long Id { get; set; }
        public long? CartId { get; set; }
        public long? OrderId { get; set; }
        public long CurrentVersionId { get; set; } = 0;
        public long UserId { get; set; }
        public long SiteId { get; set; }
        public long RetryCount { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public TransactionStatus Status { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? ModificationDate { get; set; }

        //navigation
        public Order? Order { get; set; }
        public Cart? Cart { get; set; }
        public ICollection<EcommerceLogsVersions> Versions { get; set; }
        public User User { get; set; }

    }
}
