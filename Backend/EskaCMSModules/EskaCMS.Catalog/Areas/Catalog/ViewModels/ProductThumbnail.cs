using System;

using EskaCMS.Entities.Entity.Catalog;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Areas.Core.ViewModels;
using System.Collections.Generic;
using System.Linq;
using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Tax;
using EskaCMS.Core.Models;
using EskaCMS.Entities.Enum;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductThumbnail
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Slug { get; set; }

        public decimal? Price { get; set; }
        public decimal? ForeignPrice { get; set; }
        public DateTimeOffset? CreationDate { get; set; }

        public decimal? SpecialForignPrice { get; set; }

        public decimal? SpecialPrice { get; set; }
        public EPriceType PriceType { get; set; }


        public bool IsCallForPricing { get; set; }

        public bool IsAllowToOrder { get; set; }

        public int? StockQuantity { get; set; }

        public DateTimeOffset? SpecialPriceStart { get; set; }

        public DateTimeOffset? SpecialPriceEnd { get; set; }

        public Media ThumbnailImage { get; set; }

        public string ThumbnailUrl { get; set; }
        public string thuumbnailImageUrl { get; set; }

        public bool HasVariations { get; set; }
        public int ReviewsCount { get; set; }

        public double? RatingAverage { get; set; }
        public decimal? CurrentBid { get; set; }
        public ERequestStatus Status { get; set; }
        public decimal? SellerBid { get; set; }
        public decimal? BuyerBid { get; set; }
        public DateTimeOffset? EndDate { get; set; }

        public bool IsPriceVisible { get; set; }
        public bool IsRatingVisible { get; set; }
        public TaxClass TaxClass { get; set; }
        public CalculatedItemPrice CalculatedProductPrice { get; set; }
        public IList<ItemDetailAttribute> Attributes { get; set; } = new List<ItemDetailAttribute>();
        public IList<ProductAttributeValue> AttributeValues { get; protected set; } = new List<ProductAttributeValue>();
        public List<ProductPriceWarehouse>? ProductPriceWarehouses { get; set; }=new List<ProductPriceWarehouse>();
        public decimal? TaxRate { get; set; }
        public bool InStock { get; set; }
        public long? OriginalProductId { get; set; }
        public bool IsAvailable { get; set; }

        public static ProductThumbnail FromProduct(Product product, UserCountryVM userCountry,string siteCountryId,long cultureId)
        {
            try
            {

                var productThumbnail = new ProductThumbnail();

                productThumbnail.Id = product.Id;
                productThumbnail.Name = product.ProductDetails.Where(x=> cultureId ==0|| x.CultureId== cultureId).Select(x => x.Name).FirstOrDefault();
                productThumbnail.Slug = product.ProductDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.SlugInfo.Slug).FirstOrDefault();
                productThumbnail.Price = product.Price;
                productThumbnail.ProductPriceWarehouses=product.ProductPriceWarehouses;
                productThumbnail.ForeignPrice = product.ForeignPrice;

                productThumbnail.SpecialForignPrice = product.SpecialForignPrice;
                productThumbnail.SpecialPrice = product.SpecialPrice;
                productThumbnail.SpecialPriceStart = product.SpecialPriceStart;
                productThumbnail.IsAvailable = IsAvaliableProduct(product, userCountry, siteCountryId);
                productThumbnail.SpecialPriceEnd = product.SpecialPriceEnd;
                productThumbnail.CreationDate = product.CreationDate;
                productThumbnail.IsAllowToOrder = product.IsAllowToOrder;
                productThumbnail.IsCallForPricing = product.IsCallForPricing;
                productThumbnail.ThumbnailImage = product.ThumbnailImage != null ? product.ThumbnailImage : null;
                productThumbnail.ThumbnailUrl = product.ThumbnailImage != null ? product.ThumbnailImage.PublicUrl : null;
                productThumbnail.thuumbnailImageUrl = product.ThumbnailImage != null ? product.ThumbnailImage.PublicUrl : null;
                productThumbnail.ReviewsCount = product.ReviewsCount;
                productThumbnail.RatingAverage = product.RatingAverage;
                productThumbnail.AttributeValues = product.AttributeValues;
                productThumbnail.TaxClass = product.TaxClass;
                productThumbnail.InStock = product.StockAmounts!=null? product.StockAmounts.Any(x => x.Quantity > 0):false;


                return productThumbnail;

            }
            catch (Exception)
            {

                throw;
            }
        }
        private static bool IsAvaliableProduct(Product product, UserCountryVM userCountry, string siteCountryId)
        {
            var AllowdProductBaseedOnPrice = userCountry.IsRegional ? userCountry.IsRegional : siteCountryId == userCountry.Id ? true : product.ForeignPrice.HasValue;

            return (AllowdProductBaseedOnPrice && (product.ProductAvaliablity.Count() == 0  || product.ProductAvaliablity.Any(i => i.CountryId == userCountry.Id && i.Permission == EProductCountryPermission.Allowed)));
        }
        public static ProductThumbnail FromProduct(CustomBiddingTrackerVM product)
        {
            var productThumbnail = new ProductThumbnail
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Price = product.Price,
                SpecialForignPrice = product.SpecialForignPrice,
                SpecialPrice = product.SpecialPrice,
                SpecialPriceStart = product.SpecialPriceStart,
                SpecialPriceEnd = product.SpecialPriceEnd,
                StockQuantity = product.StockQuantity,
                IsAllowToOrder = product.IsAllowToOrder,
                IsCallForPricing = product.IsCallForPricing,
                ThumbnailImage = product.ThumbnailImage,
                ReviewsCount = product.ReviewsCount,
                RatingAverage = product.RatingAverage,
                Status = product.Status,
                CurrentBid = product.CurrentBid,
                SellerBid = product.SellerBid,
                BuyerBid = product.BuyerBid,
                EndDate = product.EndDate
            };

            return productThumbnail;
        }
    }
}
