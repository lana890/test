using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class BrandDetails : EntityBase
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }

        //slug info

        public long SlugInfoId { get; set; }
        public SlugInfo SlugInfo
        {
            get { return _SlugInfo; }
            set
            {

                _SlugInfo = value;
                Slug = value == null ? string.Empty : value.Slug;
            }
        }

        [NotMapped]
        private SlugInfo _SlugInfo { get; set; }

        [NotMapped]
        public string Slug { get; set; }

        public string Description { get; set; }

        public long BrandTemplateId { get; set; }

        public virtual PageTemplates BrandTemplate { get; set; }


        public long CultureId { get; set; }
        public SitesCultures Culture { get; set; }

        public long BrandId { get; set; }
        public Brand Brand { get; set; }

    }
}
