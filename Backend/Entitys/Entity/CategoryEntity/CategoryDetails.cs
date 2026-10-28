
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Pages;

namespace EskaCMS.Entities.Entity.Core
{
    public class CategoryDetails : EntityBase
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }

        public long? SlugInfoId { get; set; }
        public SlugInfo SlugInfo
        {
            get { return _SlugInfo ?? null; }
            set
            {
                _SlugInfo = value;
                Slug = value == null ? String.Empty : value.Slug;
            }
        }
        [NotMapped]
        private SlugInfo _SlugInfo { get; set; }


        [NotMapped]
        public string Slug { get; set; }

        [StringLength(450, ErrorMessage = " Meta Title Length Must Be Under 450")]
        public string MetaTitle { get; set; }
        [StringLength(450, ErrorMessage = " Meta Keywords Length Must Be Under 450")]

        public string MetaKeywords { get; set; }
        [StringLength(450, ErrorMessage = " Meta Description Length Must Be Under 450")]

        public string MetaDescription { get; set; }

        public string Description { get; set; }
        public long? CategoryTemplateId { get; set; }

        public virtual PageTemplates CategoryTemplate { get; set; }

        public long? CultureId { get; set; }
        public virtual SitesCultures Culture { get; set; }

        public long CategoryId { get; set; }
        public Category Category { get; set; }



    }
}
