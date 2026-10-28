using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses
{
    public class CategoryWebResponseViewModel
    {
        public CategoryWebResponseViewModel(List<long> ActiveIds)
        {
            this._ActiveIds = ActiveIds ?? new List<long>();
        }
        public long Id { get; set; }
        private List<long> _ActiveIds { get; set; }

        public int DisplayOrder { get; set; }
        public List<string>? ProductNames { get; set; }

        public string PublicUrl { get; set; } 
        public string CoverImageUrl { get; set; } 
        public string TopParentSlug { get; set; }
        public string TopParentDescription { get; set; }
        public string TopParentTitle { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public bool HasProduct { get; set; }
        public string TopParentPublicURL { get; set; }
        public long TopParentId { get; set; }
        public bool Expanded { get; set; } = false;
        private List<CategoryWebResponseViewModel>? _Children { get; set; }
        public List<CategoryWebResponseViewModel>? Children
        {
            get
            {
                return this._Children;
            }
            set
            {
                this._Children = value;
                this.Expanded = this._ActiveIds.Count() == 0 ? false : this._ActiveIds.Any(x => x == this.Id) || (value != null && value.Any(x => x.Expanded));
            }
        }
        public bool? ContainsFeaturedProducts { get; set; }

    }
    public class CategoryFeatureVM
    {
        public long CategoryId { get; set; }
        public string ProductName { get; set; }
        public bool CategoryIdWithFeatured { get; set; }
    }
    public class CategoryDetailsNavigatorWebViewModel
    {
        public string TopParentSlug { get; set; }
        public string TopParentDescription { get; set; }
        public string TopParentTitle { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }



    }

    public class CategoryWebVM
    {
        public long[]? ParentIds { get; set; }
        public long CategoryTypeId { get; set; }
        public List<long> ActiveIds { get; set; }
        public OrderByEnum OrderBy { get; set; }
        public ESortType SortType { get; set; }
        public bool? ContainsFeaturedProduct { get; set; }
        public string SearchValue { get; set; }

    }
}

