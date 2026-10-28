using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductAttributeGroup : EntityBase
    {

        public long SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public IList<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
        public EStatus Status { get; set; }
        public IList<ProductAttributeGroupDetails>  productAttributeGroupDetails { get; set; }
    }

}
