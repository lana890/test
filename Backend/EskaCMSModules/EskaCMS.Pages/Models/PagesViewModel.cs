using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Pages.Models
{

    public class PagesViewModel
    {

        public long Id { get; set; }

        public string Name { get; set; }

        public long? ParentId { get; set; }
        public long PageCategoryId { get; set; }
        public long CreatedById { get; set; }
        public string PageCategoryName { get; set; }
        public long SiteId { get; set; }
        public EStatus Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }

        public List<long> RolesIds { get; set; }


        public string Route { get; set; }
        public string MetaDescription { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public string AddtionalMetaTags { get; set; }
        public bool IsLoginRequired { get; set; }
        public long? LoginPageId { get; set; }
        public long? PageTemplateId { get; set; }
        public PageTemplateVM PageTemplate { get; set; }
        public long UserId { get; set; }
        public string ThumbnailURL { get; set; }
        public long? ThumbnailId { get; set; }
        public string AdditionalContent { get; set; }
        public long? CurrentVersionId { get; set; }
        public PageVersionVM CurrentVersion { get; set; }

        public long CultureId { get; set; }
        public string CultureName { get; set; }
        public SitesCultures cultures { get; set; }

        public long DuplicatedOfId { get; set; }

        public DateTimeOffset? PublishDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public bool IsCacheable { get; set; }

    }
}
