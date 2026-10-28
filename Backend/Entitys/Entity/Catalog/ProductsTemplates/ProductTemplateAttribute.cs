

using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductTemplateAttribute : EntityBase
    {
        public long ProductTemplateId { get; set; }

        public ProductTemplate ProductTemplate { get; set; }

        public long ProductAttributeId { get; set; }

        public ProductAttribute ProductAttribute { get; set; }
    }
}
