using EskaCMS.BlogArchive.Models;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Pages.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Sites.Models
{

    public class SlugInfoVM
    {

        public long Id { get; set; }

        public string Name { get; set; }


        public long SiteId { get; set; }

        public string culture { get; set; }

        public string Route { get; set; }
        public string MetaDescription { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public bool IsLoginRequired { get; set; }
        public long? TemplateId { get; set; }
        public long? ParentId { get; set; }
        public PageTemplateVM Template { get; set; }
        public long UserId { get; set; }
        public string Image { get; set; }
        public string AdditionalContent { get; set; }
        public long? CurrentVersionId { get; set; }
        public string PostContent { get; set; }
        public string ShortDescription { get; set; }
        public DateTimeOffset PublishDate { get; set; }

        public List<TempListsObj> CategoryList { get; set; } = new List<TempListsObj>();
        public List<TempListsObj> TagList { get; set; } = new List<TempListsObj>();
        public PageVersionVM CurrentVersion { get; set; }
        public string RelatedCategoriesSettings { get; set; }
        public string PostSettings { get; set; }
        public List<PostMediaWithURLViewModel> MediaURLs { get; set; } = new List<PostMediaWithURLViewModel>();
        public string MetaTitle { get; set; }
        public SlugsTypes SlugType { get; set; }

        public ProductSlugViewModel? Product { get; set; }
        public List<ProductLinkSlugViewModel>? productLinkRVms { get; set; }
        public List<ProductLinkSlugViewModel>? productLinkCVms { get; set; }
        public string BrandName { get; internal set; }
        public string ThumbnailImageUrl { get; set; }

        public CategorySlugViewModel category { get; set; }
        public long CultureId { get;  set; }
        public long ThumbnailImageId { get; set; }
        public int Status { get; set; }


    }

    public class MetaTagsRequestViewModel
    {
        public string Slug { get; set; }

    }


    public class CategorySlugViewModel
    {
        public string DisplayOrder { get; set; }
        public bool IncludedInMenu { get; set; }
        public bool IsPublished { get; set; }
        public string FileName { get; set; }
    }
}
