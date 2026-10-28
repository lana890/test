using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductResponseViewModel
    {
        public long? Id { get; set; }
        public long StockQuantity { get; set; }
        public EStatus Status { get; set; }
        public IList<ProductAdditionalFees> ProductAdditionalFees { get; set; }
        public List<ProductDetailsResponseViewModel> ProductDetails { get; set; }
        public List<ItemMediaVm> Images { get; set; } = new List<ItemMediaVm>();
        public List<ProductAvaliablityRequest> ProductAvaliablity { get; set; } = new List<ProductAvaliablityRequest>();
        public long VendorId { get; set; }
        public DateTimeOffset? CreatedOn { get; set; }
        public EPriceType PriceType { get; set; }
        public bool IsPromotional { get; set; }

        public IList<long> ImagesIds { get; set; } = new List<long>();
        public string ThumbnailUrl { get; set; }
        public List<string> ImagelUrl { get; set; }
        public long? ThumbnailImageId { get; set; }
        public IList<long> CategoryIds { get; set; } = new List<long>();
        public EskaCMS.Entities.Enum.ProductTypes Type { get; set; }
        public decimal? BidIncrement { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? BuyoutPrice { get; set; }
        public DateTimeOffset? BidStartDate { get; set; }
        public DateTimeOffset? BidEndtDate { get; set; }
        public ERequestStatus RequestStatus { get; set; }
        public string Sku { get; set; }
        public long? TaxClassId { get; set; }
        public string Gtin { get; set; }
        public decimal? Price { get; set; }
        public decimal? ForeignPrice { get; set; }
        public decimal? SpecialPrice { get; set; }
        public DateTimeOffset? SpecialPriceStart { get; set; }
        public bool StockTrackingIsEnabled { get; set; }
        public bool IsFeatured { get; set; }
        public DateTimeOffset? SpecialPriceEnd { get; set; }
        public decimal? SpecialForignPrice { get; set; }
        public bool IsCallForPricing { get; set; }
        public bool IsVisibleIndividually { get; set; }
        public bool IsAllowForeign { get; set; }
        public bool IsAllowToOrder { get; set; }
        public bool IsProductSerial { get; set; }
        public EskaCMS.Core.Areas.Core.ViewModels.Shared.CalculatedItemPrice CalculatedItemPrice { get; set; }
        public long? BrandId { get; set; }
        public int MaxAllowedQuantity { get; set; }

        public bool IsProductVariations { get; set; }
        public bool IsPlan { get; set; }
        public List<long> selectedWarehouseIds { get; set; }


        public DateTimeOffset? PublishDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
    }

}
