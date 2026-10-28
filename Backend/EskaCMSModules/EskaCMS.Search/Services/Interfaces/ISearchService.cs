using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Models.ElasticVM;
using EskaCMS.Infrastructure.Modules.Repositories;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Search.Areas.Search.ViewModels;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EskaCMS.Search.Services.Interfaces
{
    public interface ISearchService
    {
        Task<SearchFilterListOutputVM> FullSearch(SearchFilterInputVM input);
        Task<PaginationResult<MostKeywordsVM>> GetMostSearchKeywords(SmartTableParam model);
        Task<bool> AddUserQuery(AddUserQueryVM model);
        Task<PaginationResult<GetUserQueriesDto>> GetUserQueries(SmartSearchTableParam<GetUserQueriesVM> model);
        Task<bool> DeleteUserQueries(long queryId);
        Task<NaviagtableSearchOutputModel> ElasticFullSearch(ElasticSearchInputVM input);
        Task<ProductSearchOutputModel> ProductSearch(ProductElasticSearchInput input);


    }
}
