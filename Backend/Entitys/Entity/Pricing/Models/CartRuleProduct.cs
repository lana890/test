using EskaCMS.Entities.Entity.Catalog;

namespace EskaCMS.Entities.Entity.Pricing
{
    public class CartRuleProduct
    {
        public long ProductId { get; set; }

        public Product Product { get; set; }

        public long CartRuleId { get; set; }

        public CartRule CartRule { get; set; }
    }
}
