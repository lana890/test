using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses
{
    public class CategoryAdminViewModel
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public string ParentName { get; set; }
        public bool IncludeInMenu { get; set; }
        public int DisplayOrder { get; set; }
        public long CategoryTypeId { get; set; }
        public DateTimeOffset? PublishDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public MediaViewModel? ThumbnailImage { get; set; } = new MediaViewModel();
        public MediaViewModel? CoverImage { get; set; } = new MediaViewModel();
        public EStatus Status { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPromotional { get; set; }
        public List<long>? WarehouseIds { get; set; }


        public IList<CategoryDetailsAdminViewModel> CategoryDetails { get; set; }
        public string Name { get; set; }
    }

    public class CategoryDetailsAdminViewModel

    {
        public long? Id { get; set; }

        public string Slug { get; set; }
        public long? CategoryTemplateId { get; set; }

        public ECategoryTypes CategoryTypeId { get; set; }
        public string MetaTitle { get; set; }

        public string MetaKeywords { get; set; }

        public string MetaDescription { get; set; }

        public string Description { get; set; }


        public long? CategoryId { get; set; }

        public string Name { get; set; }


        public long? TotalEvents { get; set; }
        public string CategoryType { get; set; }

        public string CultureCode { get; set; }
        public long? CultureId { get; set; }
    }

    public class MediaViewModel

    {
        public long? Id { get; set; }
        public string PublicUrl { get; set; }
        public string FileName { get; set; }
    }
}
