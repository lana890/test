using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductAttributeDetails:EntityBase
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }
        public long CultureId { get; set; }
        public EskaCMS.Entities.Entity.Core.SitesCultures Culture { get; set; }

        public long AttributeId { get; set; }
        public ProductAttribute Attribute { get; set; }
    }
}
