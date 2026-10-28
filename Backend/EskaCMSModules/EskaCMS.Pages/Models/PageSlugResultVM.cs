using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;

namespace EskaCMS.Pages.Models
{
    public class PageSlugResultVM
    {
        public long CultureId { get; set; }
        public string CultureName { get; set; }
        public string Name { get; set; }

        public string Slug { get; set; }
        public string HostName { get; set; }
        public string Prefix { get; set; }

    }

    public class FullPageWithSlugVM
    {

        public FullPageWithSlugVM()
        {

            CreationDate = DateTimeOffset.Now;
        }

        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public bool IsHidden { get; set; }
        public string NavigationTitle { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public long PageCategoryId { get; set; }
        public long? ParentId { get; set; }


        [Required(ErrorMessage = "The {0} field is required.")]
        public long SiteId { get; set; }

        public long CreatedById { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public string Route { get; set; }
        public int PageOrder { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public EStatus Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
       
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
        public virtual Site Site { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }

        public long CultureId { get; set; }
        public virtual SitesCultures Culture { get; set; }
        public ICollection<PagesRoles> PageRoles { get; set; }

        public ICollection<PageVersions> PageVersions { get; set; }
    }
}
