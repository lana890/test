
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EskaCMS.Entities.Entity.Core;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace EskaCMS.Entities.Entity.Pages
{
    [Index(nameof(Name), Name = "Nameidx")]
    [Index(nameof(NavigationTitle), Name = "NavigationTitleidx")]
    public partial class Page : EntityBase
    {


        public Page()
        {

            CreationDate = DateTimeOffset.Now;
        }


        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public bool IsHidden { get; set; }
        public string NavigationTitle { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public long PageCategoryId { get; set; }
        public long? ParentId { get; set; }

        public Page Parent { get; set; }
        public IList<Page> Children { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public long SiteId { get; set; }

        public long CreatedById { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public string Route { get; set; }
        public int PageOrder { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public long SlugInfoId { get; set; }
        public SlugInfo SlugInfo {
            get { return _SlugInfo; }
            set {

                _SlugInfo = value;
                Slug = value == null ? string.Empty : value.Slug;
            }
        }

        [NotMapped]
        private SlugInfo _SlugInfo { get; set; }

        [NotMapped]
        public string Slug { get; set; }

        public virtual Category PageCategory { get; set; }

        public long? PageTemplateId { get; set; }
        public long? CurrentVersionId { get; set; }
        public PageVersions CurrentVersion { get; set; }
        public string AddtionalMetaTags { get; set; }

        public long? ThumbnailId { get; set; }
        public virtual Media Thumbnail { get; set; }
        public string AdditionalContent { get; set; }

        public bool IsLoginRequired { get; set; }
        public virtual PageTemplates PageTemplate { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }

        public long CultureId { get; set; }
        public virtual SitesCultures Culture { get; set; }

        public long? ParentLoginPageId { get; set; }

        public Page ParentLoginPage { get; set; }

        public DateTimeOffset? PublishDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public ICollection<PagesRoles> PageRoles { get; set; }

        public ICollection<PageVersions> PageVersions { get; set; }

        public ICollection<Page> AuthorizedPages { get; set; }

        public bool IsCacheable { get; set; }

    }
}
