using System;
using System.Collections.Generic;
using System.Linq;
using static EskaCMS.Entities.Enum.GeneralEnums;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using EskaCMS.Core.Areas.Core.ViewModels;

namespace EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels
{
    public class AttributeFilterVM
    {
        public string letter { get; set; }

        public int id { get; set; }
    }
    public class SearchFilterInputVM
    {
        public string SearchValue { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }

        public int SearchOptionId { get; set; }
        public List<int> SubSearchOptions { get; set; }

        public long? CategoryId { get; set; }
        public long? SelectedAttrbuteId { get; set; }
        public long culture { get; set; }
    }
    
    public class SearchFilterOutputVM
    {
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public long? ThumbnailId { get; set; }
        public bool? IsAvailable { get; set; }
        public long? Id { get; set; }
        public long? MainProductId { get; set; }
        public string ThumbnailImageUrl { get; set;}
        public EEntity EType { get; set; }

        //ForProducts

        public IList<ItemDetailAttribute> Attributes { get; set; } = new List<ItemDetailAttribute>();

    }
    public class SearchFilterListOutputVM
    {
        public List<SearchFilterOutputVM> ListItems { get; set; }
        public int TotalLength { get; set; }
    }


    public class SearchOption
    {
        public string Query { get; set; }

        public string Brand { get; set; }

        public string Category { get; set; }
        public IList<AttributeFilterVM> attributes { get; set; }

        public string Blog { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public string Sort { get; set; }

        public int? MinPrice { get; set; }

        public int? MaxPrice { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(Query))
            {
                dict.Add("query", Query);
            }

            if (!string.IsNullOrWhiteSpace(Brand))
            {
                dict.Add("brand", Brand);
            }

            if (!string.IsNullOrWhiteSpace(Category))
            {
                dict.Add("category", Category);
            }

            if (MinPrice.HasValue)
            {
                dict.Add("minPrice", MinPrice.Value.ToString());
            }

            if (MaxPrice.HasValue)
            {
                dict.Add("maxPrice", MaxPrice.Value.ToString());
            }

            if (!string.IsNullOrWhiteSpace(Sort))
            {
                dict.Add("sort", Sort);
            }

            return dict;
        }

        public IList<string> GetBrands()
        {
            return string.IsNullOrWhiteSpace(Brand) ? new List<string>() : Brand.Split(new[] { "--" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public IList<string> GetCategories()
        {
            return string.IsNullOrWhiteSpace(Category) ? new List<string>() : Category.Split(new[] { "--" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        // Reuse one resolver instance (thread-safe, caches contracts) instead of rebuilding
        // it via reflection on every ToJson call. Same camelCase output.
        private static readonly JsonSerializerSettings _toJsonSettings =
            new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, _toJsonSettings);
        }
    }

    public class ElasticSearchInputVM
    {
        public string SearchValue { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public long culture { get; set; }
        public EEntity[] EntityTypes { get; set; }
        public bool Fuziness { get; set; }
    }

    public class ProductElasticSearchInput
    {
        public string SearchValue { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public long[]? CategoriesIds { get; set; }
    }
}
