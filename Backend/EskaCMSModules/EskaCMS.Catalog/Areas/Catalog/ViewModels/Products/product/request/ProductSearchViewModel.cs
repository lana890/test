using EskaCMS.Entities.Enum;
using System.Collections.Generic;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EEskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductSearchViewModel
    {
        public string SearchValue { get; set; }
        public long BrandId { get; set; }
        public int PageNumber { get; set; }
        public bool? IsFeatuerItem { get; set; }
        public int PageSize { get; set; }
        public ProductSearchSortViewModel ProductSort { get; set; }
        public List<long> CategoryIds { get; set; } = new List<long>();
        private long? _categoryId;

        public long? CategoryId
        {
            get => _categoryId;
            set
            {
                if (value.HasValue)
                {
                    _categoryId = value;

                    if (!CategoryIds.Contains(value.Value))
                    {
                        CategoryIds.Add(value.Value);
                    }
                }
            }
        }
        public long[]? TagsId { get; set; }
        public PriceRangeViewModel? PriceRange { get; set; }
        public List<AttributeFilter>?  AttributeFilter { get; set; }
        public AttributeSort? AttributeSort { get; set; }
        public ProductBadgeType? ProductPadgeType { get; set; }

    }

    public class AttributeFilter
    {
        public EProductSearchType SearchType { get; set; }
        public long? AttributeId { get; set; }
        public long? AttributeValueId { get; set; }
        public string? Value { get; set; }

    }


    public class FilterProductAttributeViewModel
    {
        public long? CategoryId { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }

    }
    public class GetProductAttributeViewModel
    {
        public long ProductId { get; set; }
        public long AttributeId { get; set; }
        public string Value { get; set; }
        public long AttributeValueId { get; set; }

    }
    public class GetProductAttributeGroupViewModel
    {
        public long GroupId { get; set; }
        public string GroupName { get; set; }
        public List<GetProductAttributeViewModel>  AttributeValues { get; set; }

    }

    public class AttributeSort 
    {
        public long AttributeId { get; set; }
        public ESortType SortType { get; set; }

    }

    public class ProductTagsViewModel
    {
        public EEntity EntityType { get; set; }
        public List<long>? CategoryIds { get; set; }

    }
    public class PriceRangeViewModel
    {
        public decimal? FromPrice { get; set; }
        public decimal? ToPrice { get; set; }

    }
    public class MinMaxProductsViewModel
    {
        public string Currency { get; set; }
        public PriceProductViewModel MaxpriceProduct { get; set; }
        public PriceProductViewModel MinpriceProduct { get; set; }

    }
    public class PriceProductViewModel
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }



    }
    public class ProductSearchSortViewModel
    {
        public ProductSearchSortFieldsEnum SortField { get; set; }
        public ESortType SortType { get; set; }
    }
}
