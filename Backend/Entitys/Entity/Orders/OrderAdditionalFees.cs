using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Orders
{
    public class OrderAdditionalFees : EntityBase
    {
        public long OrderId { get; set; }
        public Order Order { get; set; }

        public long ProductId { get; set; }
        public Product Product { get; set; }

        public decimal Amount { get; set; }

        public string Note { get; set; }
        public long FeesId { get; set; }
        public ProductAdditionalFees Fees { get; set; }
        public ProcessReversal Status { get; set; }
        public List<OrderAdditionalFeesHistory> OrderAdditionalFeesHistories { get; set; }
    }
}
