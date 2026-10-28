using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductTemplate : EntityBase
    {
      
        public long SiteId { get; set; }
        public Site Site { get; set; }
        public IList<ProductTemplateAttribute> TemplateAttributes { get;  set; } = new List<ProductTemplateAttribute>();
        
        public void AddAttribute(long attributeId)
        {
            var productTempateProductAttribute = new ProductTemplateAttribute
            {
                ProductTemplate = this,
                ProductAttributeId = attributeId
            };
            TemplateAttributes.Add(productTempateProductAttribute);
        }
        public List<ProductTemplateDetails> ProductTemplateDetails { get; set; }
        public EStatus Status { get; set; }

    }
}
