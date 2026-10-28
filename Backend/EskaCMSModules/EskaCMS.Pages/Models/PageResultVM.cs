using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Pages.Models
{
    public class PageResultVM
    {
        public string Name { get; set; }
        public long PageId { get; set; }
        public List<PageCultureDetailsVM> PageCultureDetails { get; set; }
        public EStatus PageStatus { get; set; }

        public long PageCategoryId { get; set; }
        public string PageCategoryName { get; set; }

        public long TemplateId { get; set; }
        public string TemplateName { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public string ParentPageSlug { get; set; }

        public long? ParentPageId { get; set; }

        public bool IsDefaultPage { get; set; }
        public bool IsRequiredLogin { get; set; }
        public DateTimeOffset? PublishDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public long SiteId { get; set; }
    }

    public class PageResultWithTotalPagesVm
    {
        public List<PageResultVM> Pages { get; set; }
        public int TotalPages { get; set; }
    }
    public class PageSiteMapResultVM
    {
        public string Name { get; set; }
        public string Culture { get; set; }
        public long CultureId { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string PageSlug { get; set; }

        public long ParentPageId { get; set; }
        public DateTimeOffset? LastModification { get; set; }


    }
    public class PageCultureDetailsVM
    {
        public string Name { get; set; }
        public long PageId { get; set; }
        public long? ParentPageId { get; set; }
        public long? ParentId { get; set; }
        public long CultureId { get; set; }
        public string CultureCode { get; set; }
        public EStatus PageCultureStatus { get; set; }
        public string Slug { get; set; }
        public string Thumbnail { get; set; }
        public long PageCategoryId { get; set; }
        public string PageCategoryName { get; set; }

        public long TemplateId { get; set; }
        public string TemplateName { get; set; }
        public LoginPageDetails LoginPage { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
    }



    public class UpdatePageRolesVM
    {
        public long ParentPageId { get; set; }
        public List<long> PageRolesList { get; set; }
    }



    public class PageIdAndCultureIdVM
    {
        public long PageId { get; set; }
        public long ParentPageId { get; set; }
        public long CultureId { get; set; }
        public string Slug { get; set; }

    }
    public class LoginPageDetails
    {
        public string Slug { get; set; }
        public string Name { get; set; }
        public long Id { get; set; }
        public long ParentId { get; set; }
    }

    public class RelatedPageVM
    {

        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsHidden { get; set; }
        public string NavigationTitle { get; set; }

        public long PageCategoryId { get; set; }
        public long? ParentId { get; set; }




        public long SiteId { get; set; }

        public long CreatedById { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public string Route { get; set; }
        public int PageOrder { get; set; }


        public GeneralEnums.EStatus Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public long SlugInfoId { get; set; }
        public SlugInfo SlugInfo { get; set; }



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


        public long CultureId { get; set; }
        public virtual SitesCultures Culture { get; set; }
        public string Slug { get; set; }
        public ICollection<PagesRoles> PageRoles { get; set; }
        public LoginPageDetails LoginPage { get; set; }
        public bool IsCacheable { get;  set; }
    }

}
