
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Discount;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Entity.ProductRecentlyViewed;
using EskaCMS.Entities.Entity.Reviews;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.Entities.Entity.Tax;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class Product : EntityBase
    {
        public Product()
        {
            Status = Enum.GeneralEnums.EStatus.Active;
            Type = Enum.ProductTypes.Product;
            BidIncrement = 0;
            MinPrice = 0;
            BuyoutPrice = 0;
            // Initialized so callers that load a Product WITHOUT .Include(ProductPriceWarehouses)
            // get an empty list instead of null. The navigation is nullable and every warehouse
            // pricing call site does ProductPriceWarehouses.Where(...), which on null throws the
            // opaque "Value cannot be null. (Parameter 'source')" from LINQ.
            ProductPriceWarehouses = new List<ProductPriceWarehouse>();
        }


        public Enum.GeneralEnums.EStatus Status { get; set; }
    
        public DateTimeOffset StatusDate { get; set; }


        public long CreatedById { get; set; }

        public User CreatedBy { get; set; }

        public DateTimeOffset CreationDate { get; set; }

        public DateTimeOffset? ModificationDate { get; set; }

        public long? ModifiedById { get; set; }

        public User ModifiedBy { get; set; }

        public List<ProductAvaliablity>? ProductAvaliablity { get; set; }

        public decimal? Price { get; set; }

        public bool IsProductSerial { get; set; }

        public decimal? SpecialPrice { get; set; }

        public decimal? SpecialForignPrice { get; set; }

        public DateTimeOffset? SpecialPriceStart { get; set; }

        public DateTimeOffset? SpecialPriceEnd { get; set; }

        public bool HasOptions { get; set; }
        public List<Stock>? Stock { get; set; }

        public bool IsVisibleIndividually { get; set; }

        public bool IsFeatured { get; set; }
        public bool IsPlan { get; set; }

        public bool IsCallForPricing { get; set; }

        public bool IsAllowToOrder { get; set; }

        public int MaxAllowedQuantity { get; set; } = 0;

        public bool StockTrackingIsEnabled { get; set; }
        [StringLength(450)]
        public string Sku { get; set; }

        [StringLength(450)]
        public string Gtin { get; set; }

        public IList<ProductDetails> ProductDetails { get; set; }

        public Enum.GeneralEnums.ERequestStatus? RequesStatus { get; set; }

        public DateTimeOffset? RequestStatusDate { get; set; }
        public Enum.ProductTypes Type { get; set; }

        public int DisplayOrder { get; set; }

        public long? VendorId { get; set; }

        public long? ThumbnailImageId { get; set; }

        public virtual Media ThumbnailImage { get; set; }

        public decimal? BidIncrement { get; set; }
        
        public decimal? ForeignPrice { get; set; }
        public EPriceType PriceType { get; set; }

        public bool IsAllowForeign { get; set; }

        public bool IncludedInReport { get; set; }
        public bool IsPromotional { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? BuyoutPrice { get; set; }
        public DateTimeOffset? BidStartDate { get; set; }
        public DateTimeOffset? BidEndtDate { get; set; }

        public DateTimeOffset? PublishDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }

        public long SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }


        public IList<ProductMedia> Medias { get; protected set; } = new List<ProductMedia>();

        public IList<ProductLink> ProductLinks { get; set; } = new List<ProductLink>();

        public IList<ProductLink> LinkedProductLinks { get; protected set; } = new List<ProductLink>();

        public IList<ProductAttributeValue> AttributeValues { get; set; } = new List<ProductAttributeValue>();

        //public IList<ProductOptionValue> OptionValues { get; protected set; } = new List<ProductOptionValue>();

        public IList<ProductCategory> Categories { get; protected set; } = new List<ProductCategory>();

        public IList<ProductPriceHistory> PriceHistories { get; protected set; } = new List<ProductPriceHistory>();

        public IList<ProductAdditionalFees> ProductAdditionalFees { get; set; } = new List<ProductAdditionalFees>();
        public List<ProductSerial> ProductSerial { get; set; } = new List<ProductSerial>();

        public int ReviewsCount { get; set; }

        public double? RatingAverage { get; set; }


        public long? BrandId { get; set; }

        public Brand Brand { get; set; }

        public long? TaxClassId { get; set; }
        public decimal? Cost { get; set; }

        public TaxClass TaxClass { get; set; }

        public List<StockAmount>? StockAmounts { get; set; }
        public List<ProductDimensions>? ProductDimensions { get; set; }

        public List<ReviewProduct> ReviewProducts { get; set; }
        public List<DiscountProduct> DiscountProducts { get; set; }
        public List<RecentlyViewedProduct> ProductViews { get; set; }
        public List<ProductOptionCombination> AvailableProductOptionValue { get; set; }
        public List<ProductBranch>? ProductBranch { get; set; }
        public List<ProductPriceWarehouse>?  ProductPriceWarehouses { get; set; }
        public List<ReviewProduct> MainProductReviewProducts { get; set; }



        #region Helper Functions

        public void AddCategory(ProductCategory category)
        {
            category.Product = this;
            Categories.Add(category);
        }

        public void AddMedia(ProductMedia media)
        {
            media.Product = this;
            Medias.Add(media);
        }

        public void AddAttributeValue(ProductAttributeValue attributeValue)
        {
            attributeValue.Product = this;
            AttributeValues.Add(attributeValue);
        }

        public void AddProductAdditionalFees(ProductAdditionalFees fees)
        {
            fees.Product = this;
            ProductAdditionalFees.Add(fees);
        }

        //public void AddOptionValue(ProductOptionValue optionValue)
        //{
        //    optionValue.Product = this;
        //    OptionValues.Add(optionValue);
        //}

        public void AddProductLinks(ProductLink productLink)
        {
            productLink.Product = this;
            ProductLinks.Add(productLink);
        }

        public IList<ProductOptionCombination> OptionCombinations { get; protected set; } = new List<ProductOptionCombination>();

        public void AddOptionCombination(ProductOptionCombination combination)
        {
            combination.Product = this;
            OptionCombinations.Add(combination);
        }

        public Product Clone()
        {
            var product = new Product();
            product.Price = Price;
            product.SpecialForignPrice = SpecialForignPrice;
            product.SpecialPrice = SpecialPrice;
            product.SpecialPriceStart = SpecialPriceStart;
            product.SpecialPriceEnd = SpecialPriceEnd;
            product.HasOptions = HasOptions;
            product.IsVisibleIndividually = IsVisibleIndividually;
            product.IsFeatured = IsFeatured;
            product.IsAllowToOrder = IsAllowToOrder;
            product.IsCallForPricing = IsCallForPricing;
         //   product.StockQuantity = StockQuantity;
            product.BrandId = BrandId;
            product.VendorId = VendorId;
            product.TaxClassId = TaxClassId;
            product.StockTrackingIsEnabled = StockTrackingIsEnabled;
            product.DisplayOrder = DisplayOrder;
            product.TaxClassId = TaxClassId;

            foreach (var category in Categories)
            {
                product.AddCategory(new ProductCategory
                {
                    CategoryId = category.CategoryId
                });
            }

            return product;
        }

        #endregion
    }
}
