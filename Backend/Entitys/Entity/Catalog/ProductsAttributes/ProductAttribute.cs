using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductAttribute : EntityBase
    {
   

        public long GroupId { get; set; }
        public bool IsVisible { get; set; }
        public long SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public ProductAttributeGroup Group { get; set; }
        public IList<ProductAttributeDetails> AttributeDetails { get; set; }    

        public IList<ProductTemplateAttribute> ProductTemplates { get; protected set; } = new List<ProductTemplateAttribute>();
        public Enum.GeneralEnums.EStatus Status { get; set; }
        public IList<ProductAttributeValue> AttributeValues { get; set; } = new List<ProductAttributeValue>();

    }
}
