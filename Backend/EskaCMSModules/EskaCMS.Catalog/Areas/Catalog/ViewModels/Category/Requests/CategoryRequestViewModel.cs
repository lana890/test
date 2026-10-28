using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.ViewModels.Category
{
    public class CategoryRequestViewModel
    {
        public long Id { get; set; }
        public long? ThumbnailImageId { get; set; }
        public long? CoverImageId { get; set; }
        public int DisplayOrder { get; set; }
        public long? ParentId { get; set; }
        public long CategoryTypeId { get; set; }
        public bool IncludeInMenu { get; set; }
        public EStatus Status { get; set; }
        public DateTimeOffset? PublishDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPromotional { get; set; }
        public IList<long>? WarehouseIds { get; set; }


        public IList<CategoryDetailsViewModel> CategoryDetails { get; set; }
    }
}
