using EskaCMS.Infrastructure.Enums;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace EskaCMS.Infrastructure.Modules.Repositories;

public interface ISearchRepository<T>
{
    Task<SearchResponse<T>> FullTextSearch(string search, SearchRequestConfig searchRequestConfig);
    Task<SearchResponse<T>> ProductSearch(string search, ProductSearchRequestConfig searchRequestConfig);
    Task DeleteDocuemnt(string id, string type=null );
    Task DeleteIndex(string type=null);
  


}
public class SearchRequestConfig
{
    public bool Fuziness { get; set; }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public string? EntityType { get; set; }
    public long? SiteId { get; set; }
    public long? CultureId { get; set; }
    public List<KeyValuePair<string, object>> Options =new List<KeyValuePair<string, object>>();
}
public class ProductSearchRequestConfig
{
    public int PageSize { get; set; }
    public bool Fuziness { get; set; }

    public int PageNumber { get; set; }
    public long? SiteId { get; set; }
    public long? CultureId { get; set; }
    public long[] CategoriesIds { get; set; }

    public List<KeyValuePair<string, object>> Options = new List<KeyValuePair<string, object>>();

}

public class SearchResponse<T>
{
    public IEnumerable<T> Result = Enumerable.Empty<T>();
    public long Count { get; set; }
}