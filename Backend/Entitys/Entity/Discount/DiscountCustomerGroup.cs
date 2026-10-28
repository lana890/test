using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pricing;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Discount
{
    public class DiscountCustomerGroup:EntityBase
    {
        public long DiscountId { get; set; }

        public Discount  Discount { get; set; }

        public long CustomerGroupId { get; set; }

        public CustomerGroup CustomerGroup { get; set; }
    }
}
