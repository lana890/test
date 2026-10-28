
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.Pricing
{
    public class CartRuleCustomerGroup:EntityBase
    {
        public long CartRuleId { get; set; }

        public CartRule CartRule { get; set; }

        public long CustomerGroupId { get; set; }

        public CustomerGroup CustomerGroup { get; set; }
    }
}
