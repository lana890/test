using EskaCMS.BlogArchive.Models;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using EskaCMS.Pages.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Sites.Services.Model
{
    public class SlugInfoSettings
    {

            public PostAndPageViewModel? PageSettings { get; set; }=new PostAndPageViewModel();
            public PostAndPageViewModel? PostSettings { get; set; }=new PostAndPageViewModel();
            public ProductViewModel? ProductSettings { get; set; }=new ProductViewModel();
            public CategoryViewModel? CategoriesSettings { get; set; }=new CategoryViewModel();

            public BrandViewModel? BrandSettings { get; set; }=new BrandViewModel();

            public SlugsTypes SlugType { get; set; }






        }
    public class DefaultSlugDto
    {
        public long PageId { get; set; }
        public string? Slug { get; set; }
        public string? Prefix { get; set; }
    }






    public class PostAndPageViewModel
        {


            public long Id { get; set; }

            public string Name { get; set; }

            public string LoginSlug { get; set; }
            public long SiteId { get; set; }

            public string culture { get; set; }
            public string EncryptedUserId { get; set; }

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
            public int? ImageWidth { get; set; }    // NEW
            public int? ImageHeight { get; set; }   // NEW
        public SlugsTypes SlugType { get; set; }




    }

    public class ProductViewModel
        {

            public long Id { get; set; }

            public string Name { get; set; }

            public string ShortDescription { get; set; }

            public string AdditonalMetaTags { get; set; }
            public string ThumbnailImageUrl { get; set; }
            public int? ImageWidth { get; set; }    // NEW
            public int? ImageHeight { get; set; }   // NEW

            public long SiteId { get; set; }

            public string Description { get; set; }

        
            public string Slug { get; set; }

            public SlugsTypes SlugType { get; set; }
           
            public long? TemplateId { get;  set; }
             public long? ParentId { get;  set; }
             public long? CultureId { get; internal set; }
    }


    
    public class BrandViewModel
        {
            public long Id { get; set; }

            public string Slug { get; set; }

            public string Name { get; set; }

            public string Description { get; set; }
            public bool IsPublished { get; set; }
            public EStatus Status { get; set; }

            public long? ParentId { get; set; }

            public long CultureId { get; set; }
            public long BrandTemplateId { get; set; }
            public long? ThumbnailId { get; set; }
            public string? PublicUrl { get; set; }
            public int? ImageWidth { get; set; }    // NEW
            public int? ImageHeight { get; set; }   // NEW
        public SlugsTypes SlugType { get; set; }

    }
}

