using EskaCMS.Entities.Entity.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductAttributeViewModel
    {
        public long? Id { get; set; }
        public long GroupId { get; set; }
        public bool IsVisible { get; set; }
        public long SiteId { get; set; }

        public ProductAttributeGroup Group { get; set; }

        public string DefaultGroupName { get; set; }
        public IList<ProductAttributeDetails> AttributeDetails { get; set; }

        public IList<ProductTemplateAttribute> ProductTemplates { get;  set; } = new List<ProductTemplateAttribute>();
        public EStatus Status { get; set; }
    }
}
