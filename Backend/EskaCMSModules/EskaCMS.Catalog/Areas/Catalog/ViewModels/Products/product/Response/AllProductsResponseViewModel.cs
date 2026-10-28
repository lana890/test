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
    public class AllProductsResponseViewModel
    {
        public bool IsAllowForeign { get; set; }

        public long? Id { get; set; }
        public List<ProductStockQuantity> Stock { get; set; }
        public EStatus Status { get; set; }
        public IList<ProductAdditionalFees> ProductAdditionalFees { get; set; }
        public DateTimeOffset? CreatedOn { get; set; }

        public string DefaultName { get; set; }
        public List<ProductDetailsResponseViewModel> ProductDetails { get; set; }
        public long VendorId { get; set; }

        public EskaCMS.Entities.Enum.ProductTypes Type { get; set; }
        public EPriceType PriceType { get; set; }

        public long? TaxClassId { get; set; }
        public decimal? Price { get; set; }
        public decimal? ForeignPrice { get; set; }
        public decimal? SpecialPrice { get; set; }
        public bool StockTrackingIsEnabled { get; set; }
        public bool IsFeatured { get; set; }
        public string Sku { get; set; }

        public string Gtin { get; set; }
        public decimal? SpecialForignPrice { get; set; }
        public bool IsCallForPricing { get; set; }
        public bool IsAllowToOrder { get; set; }

        public IList<AllProductsResponseViewModel> Variations { get; set; }
        public bool HasOptions { get;  set; }
        public bool IsVisibleIndividually { get;  set; }
        public bool IsPlan { get;  set; }

        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public string Thumbnail { get; set; }
        public bool IsPublished { get;  set; }
        public bool IsPromotional { get; set; }


    }

    public class ProductStockQuantity
    {
        public string WareHouseName { get; set; }
        public int Quantity { get; set; }

    }
}
