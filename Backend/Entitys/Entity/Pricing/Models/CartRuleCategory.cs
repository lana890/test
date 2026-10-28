using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Pricing
{
    public class CartRuleCategory
    {
        public long CategoryId { get; set; }

        public Category Category { get; set; }

        public long CartRuleId { get; set; }

        public CartRule CartRule { get; set; }
    }
}
