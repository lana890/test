using static EskaCMS.Entities.Enum.GeneralEnums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Models
{
    public class CategoryViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long? UserId { get; set; }
        public long? SiteId { get; set; }
        public long? CreatedById { get; set; }

        public long? CategoryTemplateId { get; set; }

        public long? ThumnailImageId { get; set; }

        //[Required(ErrorMessage = "The {0} field is required.")]
        //[StringLength(450)]
        public string Slug { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public ECategoryTypes CategoryTypeId { get; set; }

        public string MetaTitle { get; set; }

        public string MetaKeywords { get; set; }

        public string MetaDescription { get; set; }

        public string Description { get; set; }

        public int DisplayOrder { get; set; }

        public EStatus Status { get; set; }
        public bool IsPublished { get; set; }

        public bool IncludeInMenu { get; set; }

        public MediaCategoryVM? MediaCategory { get; set; }

        public long? ParentId { get; set; }

        public long? TotalEvents { get; set; }
        public long? CultureId { get; set; }
        public string CategoryType { get; set; }
        public string CultureName { get; set; }

        public long? CultureParentId { get; set; }
        public string FileName { get; set; }
        public string PublicUrl { get; set; }
        public int? ImageWidth { get; set; }    // NEW
        public int? ImageHeight { get; set; }   // NEW
        public SlugsTypes? SlugType { get; set; } = null;
        public List<ProductCategoryVM>? ProductCategory { get; set; } = new List<ProductCategoryVM>();

    }

    public class MediaCategoryVM

    {
        public long? Id { get; set; }
        public string PublicUrl { get; set; }
        public string FileName { get; set; }
    }
    public class ProductCategoryVM
    {


        public string Sku { get; set; }


        public string Description { get; set; }

        public string Specification { get; set; }

        public bool IsCallForPricing { get; set; }

        public bool IsAllowToOrder { get; set; }

        public bool StockTrackingIsEnabled { get; set; }

        public string Slug { get; set; }
        public int StockQuantity { get; set; }

        public int ReviewsCount { get; set; }

        public long Id { get; set; }

        public string Name { get; set; }

        public string ShortDescription { get; set; }

        public string ProductAdditionalMetatags { get; set; }

        
        public string ThumbnailImageUrl { get; set; }
        public double? RatingAverage { get; set; }

        public long SiteId { get; set; }


    }

}
