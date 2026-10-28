using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Core.Areas.Core.ViewModels.Tags;
using EskaCMS.Entities.Entity;
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
    public class ProductViewModel
    {
        public long Id { get; set; }
        public long ParentId { get; set; }
        public long VendorId { get; set; }

        public string Name { get; set; }

        public string Name2 { get; set; }

        public string Slug { get; set; }
        public string ThumbnailUrl { get; set; }

        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string Specification { get; set; }
        public EPriceType PriceType { get; set; }
        public bool IsPromotional { get; set; }

        public decimal Price { get; set; }
        public decimal? ForeignPrice { get; set; }
        public EskaCMS.Entities.Enum.ProductTypes Type { get; set; }
        public decimal? BidIncrement { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? BuyoutPrice { get; set; }
        public DateTimeOffset? BidStartDate { get; set; }
        public DateTimeOffset? BidEndtDate { get; set; }
        public ERequestStatus RequestStatus { get; set; }
        public long? ProductTemplateId { get; set; }
        public bool StockTrackingIsEnabled { get; set; }
        public List<ItemDetailVariationOptionAdmin>? Options { get; set; } = new List<ItemDetailVariationOptionAdmin>();
        public List<ProductAttribueWithvaluesModel> AttributeValues { get; set; }=new List<ProductAttribueWithvaluesModel>();
        public List<ProductAttribueWithvaluesVM> Attribute { get; set; } = new List<ProductAttribueWithvaluesVM>();
        public bool IsFeatured { get;  set; }
        public bool IsPlan { get;  set; }
        public string AddtionalMetaTags { get;  set; }
    }
    public class ProductAttribueWithvaluesVM
    {
        public string Name { get; set; }
        public long Id { get; set; }
    }
    public class ProductAttribueWithvaluesModel
    {
        public string value { get; set; }
        public long Id { get; set; }
        public long AttributeId { get; set; }
    }


    public class ProductWebViewModel
    {
        public long Id { get; set; }
        public long ParentId { get; set; }
        public long VendorId { get; set; }

        public CalculatedItemPrice Price { get; set; }

        public string Name { get; set; }

        public string Name2 { get; set; }
        public bool IsPromotional { get; set; }

        public string Slug { get; set; }
        public string CategorySlug { get; set; }
        public string ThumbnailUrl { get; set; }
        public bool HasVariations { get; set; }
        public string MetaTitle { get; set; }

        public string MetaKeywords { get; set; }
        public string MetaDescription { get; set; }

        public bool InStock { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string Specification { get; set; }

        //public decimal Price { get; set; }
        public string Type { get; set; }
        public decimal? BidIncrement { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? BuyoutPrice { get; set; }
        public DateTimeOffset? BidStartDate { get; set; }
        public DateTimeOffset? BidEndtDate { get; set; }
        public ERequestStatus RequestStatus { get; set; }
        public long? ProductTemplateId { get; set; }
        public bool StockTrackingIsEnabled { get; set; }
        public List<ItemDetailVariationOption>? Options { get; set; } = new List<ItemDetailVariationOption>();
        public List<GetTagsViewModel>?  EntityTags { get; set; } = new List<GetTagsViewModel>();
        public List<ProductAttribueWithvaluesModel> AttributeValues { get; set; }
        public List<ProductAttribueWithvaluesVM> Attribute { get; set; }
        public bool IsFeatured { get;  set; }
        public double Rating { get; set; }
       public int RatingCount { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public string AddtionalMetaTags { get;  set; }
        public int DisplayOrder { get;  set; }
        public bool IsAvailable { get;  set; }
        public GetTagsViewModel? Tag {  get; set; }
        public DateTimeOffset? OfferEndsAt { get; set; }
        public List<CategoryProductWarehouse> categoryProductWarehouses { get;  set; }=new List<CategoryProductWarehouse>();
        public List<CategoryProductWarehouse> ParentcategoryProductWarehouses { get;  set; }=new List<CategoryProductWarehouse>();
    }
    public class CategoryProductWarehouse {

        public long WarehouseId { get; set; }
        public long CategoryId { get; set; }

    }
}
