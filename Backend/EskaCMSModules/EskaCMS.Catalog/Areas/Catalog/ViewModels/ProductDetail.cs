using System.Collections.Generic;
using System.Linq;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Media.Models;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductDetail
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string ShortDescription { get; set; }


        public string ThumbnailImageUrl { get; set; }
        public string BrandName { get; set; }
        public bool InStock { get; set; }
        public bool IsPromotional { get; set; }



        public CalculatedItemPrice CalculatedItemPrice { get; set; }

        public string Description { get; set; }

        public string Specification { get; set; }

        public bool IsCallForPricing { get; set; }

        public bool IsAllowToOrder { get; set; }

        public bool StockTrackingIsEnabled { get; set; }

        public int StockQuantity { get; set; }

        public int ReviewsCount { get; set; }

        public double? RatingAverage { get; set; }

        public bool HasVariation => Variations.Any();

        public IList<ItemDetailOption> AvailableOptions
        {
            get
            {
                var options = from opt in Variations.SelectMany(x => x.Options)
                              group opt by new
                              {
                                  opt.OptionId,
                                  opt.OptionName
                              }
                    into g
                              select new ItemDetailOption
                              {
                                  OptionId = g.Key.OptionId,
                                  OptionName = g.Key.OptionName,
                                  Values = g.Select(x => x.Value).Distinct().ToList()
                              };

                return options.ToList();
            }
        }

        public IDictionary<string, ItemOptionDisplay> OptionDisplayValues { get; set; } = new Dictionary<string, ItemOptionDisplay>();

        public IList<MediaViewModel> Images { get; set; } = new List<MediaViewModel>();

        public IList<string> ImagesUrl { get; set; } = new List<string>();
        public IList<long> ImagesIds { get; set; } = new List<long>();

        public IList<ItemDetailVariation> Variations { get; set; } = new List<ItemDetailVariation>();

        public IList<ItemDetailAttribute> Attributes { get; set; } = new List<ItemDetailAttribute>();

        public IList<ProductFeesVM> AdditionalFees { get; set; } = new List<ProductFeesVM>();

        public IList<ItemDetailCategory> Categories { get; set; } = new List<ItemDetailCategory>();

        public IList<ProductThumbnail> RelatedProducts { get; set; } = new List<ProductThumbnail>();

        public IList<ProductThumbnail> CrossSellProducts { get; set; } = new List<ProductThumbnail>();
        public IList<ProductThumbnail> SubProducts { get; set; } = new List<ProductThumbnail>();

        public IList<BiddingTrackerVM> BiddingHistory { get; set; } = new List<BiddingTrackerVM>();
        public string Sku { get; set; }
        public string AddtionalMetaTags { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class ViewProductDetailaVM
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string ShortDescription { get; set; }


        public string ThumbnailImageUrl { get; set; }
        public string BrandName { get; set; }
        public bool InStock { get; set; }



        public CalculatedItemPrice CalculatedItemPrice { get; set; }

        public string Description { get; set; }

        public string Specification { get; set; }

        public bool IsCallForPricing { get; set; }

        public bool IsAllowToOrder { get; set; }

        public bool StockTrackingIsEnabled { get; set; }

        public int StockQuantity { get; set; }

        public int ReviewsCount { get; set; }

        public double? RatingAverage { get; set; }


        public IList<string> ImagesUrl { get; set; } = new List<string>();
        public IList<long> ImagesIds { get; set; } = new List<long>();

   
        public IList<ProductFeesVM> AdditionalFees { get; set; } = new List<ProductFeesVM>();

    
        public string Sku { get; set; }
        public string AddtionalMetaTags { get; set; }
        public bool IsAvailable { get;  set; }
    }


    public class ProductReviewDetailsVM
    {

        public int ReviewsCount { get; set; }

        public double? RatingAverage { get; set; }


    }
    public class ProductAttributeDetailsVM
    {
        public IList<ItemDetailAttribute> Attributes { get; set; } = new List<ItemDetailAttribute>();

    }
    
    public class ProductCategoryDetailsVM
    {
        public IList<ItemDetailCategory> Categories { get; set; } = new List<ItemDetailCategory>();


    }

    public class LinkedProductDetailsVM
    {

        public IList<ProductThumbnail> RelatedProducts { get; set; } = new List<ProductThumbnail>();

        public IList<ProductThumbnail> CrossSellProducts { get; set; } = new List<ProductThumbnail>();
        public IList<ProductThumbnail> SubProducts { get; set; } = new List<ProductThumbnail>();



    }

    public class ProductOptionDetailsVM
    {

        public IList<ItemDetailOption> AvailableOptions { get; set; }=new List<ItemDetailOption>();

    }
     public class ProductVariationDetailsVM
    {

        public IList<ItemDetailVariation> Variations { get; set; } = new List<ItemDetailVariation>();


    }



















}
