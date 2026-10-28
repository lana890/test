using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductTemplateDetails : EntityBase
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }
       
        public long CultureId { get; set; }
        public SitesCultures Culture { get; set; }
        public long ProductTemplateId { get; set; }
        public ProductTemplate ProductTemplate { get; set; }

    }
}
