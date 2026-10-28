using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses
{
    public class CategoryWebViewModel
    {
       
            public long Id { get; set; }
            public long? ParentId { get; set; }
            public bool IncludeInMenu { get; set; }
            public int DisplayOrder { get; set; }
            public long CategoryTypeId { get; set; }

            public MediaViewModel ThumbnailImage { get; set; }
            public MediaViewModel CoverImage { get; set; }
            public EStatus Status { get; set; }

            public string Slug { get; set; }
            public long? CategoryTemplateId { get; set; }

            public string MetaTitle { get; set; }

            public string MetaKeywords { get; set; }

            public string MetaDescription { get; set; }

            public string Description { get; set; }


            public string Name { get; set; }

        public bool IsFeatured { get; set; }
        public bool IsPromotional { get; set; }


        public string CultureCode { get; set; }
            public long? CultureId { get; set; }

    }
}
