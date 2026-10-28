using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;


namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemDetailVariation
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string NormalizedName { get; set; }
        public bool HasSub { get; set; }
        public bool HasRelated { get; set; }
        public bool InStock { get; set; }

        public bool IsCallForPricing { get; set; }

        public bool IsAllowToOrder { get; set; }

        public int StockQuantity { get; set; }

        public bool StockTrackingIsEnabled { get; set; }
        public string ShortDescription { get; set; }
        public double? RatingAverage { get; set; }

        public bool IsAvailable { get; set; }
        public string Description { get; set; }
        public decimal? SpecialPrice { get; set; }
        public DateTimeOffset? SpecialPriceStart { get; set; }

        public DateTimeOffset? SpecialPriceEnd { get; set; }
        public CalculatedItemPrice CalculatedItemPrice { get; set; }

        public IList<ItemDetailVariationOption> Options { get;  set; } = new List<ItemDetailVariationOption>();

        public IList<ItemVariationFeesVM> AdditionalFees { get; set; } = new List<ItemVariationFeesVM>();

        public IList<ItemDetailAttribute> Attributes { get; set; } = new List<ItemDetailAttribute>();

        public IList<string> ImagesUrl { get; set; } = new List<string>();
        public IList<long> ImagesIds { get; set; } = new List<long>();
        public int ReviewsCount { get; set; }
    }
}
