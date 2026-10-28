using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Discount;
using EskaCMS.Entities.Entity.Reviews;
using EskaCMS.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{

    public class ProductQueryViewModel
    {
        public Product Product { get; set; }
        public EskaCMS.Entities.Entity.Catalog.ProductDetails Details { get; set; }
        public string Slug { get; set; } = "default-slug";
        public List<CategoryQueryViewModel> Categories { get; set; } = new List<CategoryQueryViewModel>();
        public Media Thumbnail { get; set; }
        public List<StockAmount> Stock { get; set; } = new List<StockAmount>();
        public List<ProductAvaliablity> Availability { get; set; } = new List<ProductAvaliablity>(); // Corrected typo
        public List<ReviewProduct> MainReviews { get; set; } = new List<ReviewProduct>();
        public List<DiscountProduct> Discounts { get; set; } = new List<DiscountProduct>();
        public IList<ProductLink> Links { get; set; } = new List<ProductLink>();
        public List<ProductPriceWarehouse> Prices { get; set; } = new List<ProductPriceWarehouse>();
        public IList<ProductAttributeValue> Attributes { get; set; } = new List<ProductAttributeValue>();
        public Brand Brand { get; set; }
        public EPriceType PriceType { get; set; }
        public decimal? PriceValue { get; set; }
        public string DetailName { get;  set; }
    }

    // ViewModel for Categories collection
    public class CategoryQueryViewModel
    {
        public long CategoryId { get; set; }
        public string Name { get; set; } = "Unknown";
    }
    public class ProductRequestViewModel
    {
        public long ? Id { get; set; }
        public long StockQuantity { get; set; }
        public EStatus Status { get; set; }
        public IList<ProductAdditionalFees> ProductAdditionalFees { get; set; }
        public List<ProductDetailsViewModel> ProductDetails { get; set; }
        public long? VendorId { get; set; }
        public bool IsProductSerial { get; set; }
        public EPriceType PriceType { get; set; }

        public IList<long> ImagesIds { get; set; } = new List<long>();
        public Dictionary<long,int> Images { get; set; }
        public string ThumbnailUrl { get; set; }
        public bool applyForVariations { get; set; }
        public bool applyStockForVariations { get; set; }
        public List<string> ImagelUrl { get; set; }
        public long? ThumbnailImageId { get; set; }
        public IList<long> CategoryIds { get; set; } = new List<long>();
        public List<ProductAvaliablityRequest> ProductAvaliablity { get; set; } = new List<ProductAvaliablityRequest>();
        public EskaCMS.Entities.Enum.ProductTypes Type { get; set; }
        public decimal? BidIncrement { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? BuyoutPrice { get; set; }
        public string Sku { get; set; }

        public string Gtin { get; set; }
        public DateTimeOffset? BidStartDate { get; set; }
        public DateTimeOffset? BidEndtDate { get; set; }
        public ERequestStatus RequestStatus { get; set; }
        public long? TaxClassId { get; set; }
        public decimal? Price { get; set; }
        public decimal? SpecialPrice { get; set; }
        public DateTimeOffset? SpecialPriceStart { get; set; }
        public bool StockTrackingIsEnabled { get; set; }
        public bool IsFeatured { get; set; }
        public DateTimeOffset? SpecialPriceEnd { get; set; }
        public decimal? SpecialForignPrice { get; set; }
        public decimal? ForeignPrice { get; set; }
        public bool? IsCallForPricing { get; set; }
        public bool IsAllowToOrder { get; set; }
        public bool IsAllowForeign { get; set; }
        public IList<ItemOptionCombinationVm>? OptionCombinations { get; set; } =
          new List<ItemOptionCombinationVm>();
        public EskaCMS.Core.Areas.Core.ViewModels.Shared.CalculatedItemPrice CalculatedItemPrice { get; set; }
        public long? BrandId { get; set; }
        public int DisplayOrder { get; internal set; }
        public int MaxAllowedQuantity { get;  set;}
        public bool? IsVisibleIndividually { get; set; }
        public bool IsPlan { get;  set; }
        public bool? AddStockForAllWarehouses { get;  set; }
        public int? StockForAllWarehouses { get; set; } = 9999;
        public List<long>? SelectedWarehouseIds { get; set; }
        public DateTimeOffset? PublishDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public bool IsPromotional { get; set; }


    }

    public class ProductAvaliablityRequest
    {
        public EProductCountryPermission Permission { get; set; }
        public string CountryId { get; set; }
        public long? StateOrProvinceId { get; set; }
    }




}
