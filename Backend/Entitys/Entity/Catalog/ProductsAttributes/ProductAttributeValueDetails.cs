using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Catalog.ProductsAttributes
{
    public class ProductAttributeValueDetails:EntityBase
    {

        public string Value { get; set; }

        public long CultureId { get; set; }
        public EskaCMS.Entities.Entity.Core.SitesCultures Culture { get; set; }


        public long AttributeValueId { get; set; }

        public ProductAttributeValue AttributeValue { get; set; }
    }
}
