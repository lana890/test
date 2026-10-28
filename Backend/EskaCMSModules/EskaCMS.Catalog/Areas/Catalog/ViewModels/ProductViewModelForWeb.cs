using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Core.Areas.Core.ViewModels.Tags;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductViewModelForWeb
    {
        public string Catname { get; set; }

        public PaginationResult<ProductForWeb> products { get; set; }

    }


    public class ProductForWeb
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }

        public string ThuumbnailImageUrl { get; set; }
        public long Id { get; set; }
        public CalculatedItemPrice Price { get; set; }

        public bool IsCallForPricing { get;  set; }
        public string Slug { get;  set; }
        public string PriceCurrency { get;  set; }
        public bool IsFeatured { get;  set; }
        public bool IsAvailable { get; internal set; }
        public bool InStock { get; internal set; }
        public bool HasVariations { get; internal set; }
        public bool IsPromotional { get; set; }
         public GetTagsViewModel? Tag { get; set; }
        public string Type {  get; set; }
        public double? Rating {  get; set; }
        public int? RatingCount {  get; set; }
        public DateTimeOffset? OfferEndsAt {  get; set; }


    }
}
