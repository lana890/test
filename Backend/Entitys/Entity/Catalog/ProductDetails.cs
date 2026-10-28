using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EskaCMS.Entities.Entity.Catalog
{
    [Index(nameof(Name), Name = "Nameidx")]
   
    public class ProductDetails:EntityBase
    {
        [StringLength(450)]
        public string NormalizedName { get; set; }// c
        [StringLength(450)]
        public string ShortDescription { get; set; }// c

        public string Description { get; set; }// c

        public string Specification { get; set; } // ?
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }

        public long? ProductTemplateId { get; set; }

        public virtual PageTemplates ProductTemplate { get; set; }

        [StringLength(450)]
        public string Name2 { get; set; }

        public string AddtionalMetaTags { get; set; }

        public long SlugInfoId { get; set; }

        public SlugInfo  SlugInfo { get; set; }


        public long CultureId { get; set; }

        public SitesCultures Culture { get; set; }

        public long ProductId { get; set; }
        public Product Product { get; set; }
    }
}
