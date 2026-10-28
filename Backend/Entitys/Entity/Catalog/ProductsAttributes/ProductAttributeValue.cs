
using EskaCMS.Infrastructure.Models;
using EskaCommerce.Infrastructure.Models;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog.ProductsAttributes;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductAttributeValue : EntityBase
    {
        public long AttributeId { get; set; }

        public ProductAttribute Attribute { get; set; }

        public long ProductId { get; set; }

        public Product Product { get; set; }

        public List<ProductAttributeValueDetails> AttributeValueDetails { get; set; }
    }
}
