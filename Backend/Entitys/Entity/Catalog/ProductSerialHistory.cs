using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductSerialHistory:EntityBase
    {
       
        public EskaCMS.Entities.Enum.GeneralEnums.EProductSerialStatus OldStatus { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EProductSerialStatus NewStatus { get; set; }
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string Note { get; set; }
        public long?  OrderId { get; set; }
        public Order Order { get; set; }
        public long ProductSerialId { get; set; }
        public ProductSerial ProductSerial { get; set; }
    }
}
