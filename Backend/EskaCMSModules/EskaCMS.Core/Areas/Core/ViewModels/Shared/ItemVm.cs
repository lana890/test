using EskaCMS.Module.Core.Areas.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemVm
    {
        public ItemVm()
        {
            IsPublished = true;
            IsCallForPricing = false;
            IsAllowToOrder = true;
            Price = 0;
        }

        public long Id { get; set; }

        public decimal Price { get; set; }

        public decimal? OldPrice { get; set; }

        public decimal? SpecialPrice { get; set; }

        public DateTimeOffset? SpecialPriceStart { get; set; }

        public DateTimeOffset? SpecialPriceEnd { get; set; }

        public bool IsCallForPricing { get; set; }

        public bool IsAllowToOrder { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name2 { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Slug { get; set; }

        public string MetaTitle { get; set; }

        public string MetaKeywords { get; set; }

        public string MetaDescription { get; set; }

        public string Sku { get; set; }

        public string Gtin { get; set; }

        public string ShortDescription { get; set; }

        public string Description { get; set; }

        public string Specification { get; set; }

        public bool IsPublished { get; set; }

        public bool IsFeatured { get; set; }

        public bool StockTrackingIsEnabled { get; set; }

        public IList<long> CategoryIds { get; set; } = new List<long>();

        public IList<ItemAttributeVm> Attributes { get; set; } = new List<ItemAttributeVm>();

        public IList<ItemOptionVm> Options { get; set; } = new List<ItemOptionVm>();

        public IList<ItemVariationVm> Variations { get; set; } = new List<ItemVariationVm>();

        public string ThumbnailImageUrl { get; set; }

        public IList<ItemMediaVm> ItemImages { get; set; } = new List<ItemMediaVm>();

        public IList<ItemMediaVm> ItemDocuments { get; set; } = new List<ItemMediaVm>();

        public IList<long> DeletedMediaIds { get; set; } = new List<long>();

        public long? BrandId { get; set; }

        public long? TaxClassId { get; set; }

        public List<ItemLinkVm> RelatedItems { get; set; } = new List<ItemLinkVm>();

        public List<ItemLinkVm> CrossSellItems { get; set; } = new List<ItemLinkVm>();
    }
}
