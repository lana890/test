using System.Collections.Generic;
using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Entities.Entity;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemVariationVm
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string NormalizedName { get; set; }

        public string Sku { get; set; }

        public string Gtin { get; set; }

        public decimal? Price { get; set; }

        public decimal? OldPrice { get; set; }

        public string ThumbnailImageUrl { get; set; }

        public long? ThumbnailImageId { get; set; }
        public long CultureId { get; set; }
        public string CultureCode { get; set; }
        public long? ProductTemplateId { get; set; }
        public CalculatedItemPrice CalculatedItemPrice { get; set; }
        public long ParentId { get; set; }
        public IList<ItemOptionCombinationVm>? OptionCombinations { get; set; } =
            new List<ItemOptionCombinationVm>();
        public bool IsVisibleIndividually { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsCallForPricing { get; set; }

        public bool IsAllowToOrder { get; set; } = true;
        public bool IsPublished { get; set; } = true;
    }
}
