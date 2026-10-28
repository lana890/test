using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Models.MeiliSearchVM;
using EskaCMS.Infrastructure.Modules.Repositories;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Search.Areas.Search.ViewModels;
using EskaCMS.Search.Model;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using System.Threading.Tasks;

namespace EskaCMS.Search.Services.Interfaces
{
    public interface IMeiliSearchService
    {
        Task<NaviagtableSearchOutputModel> MeiliFullSearch(MeiliSearchInputVM input);

        //Task<NaviagtableSearchOutputModel> ElasticFullSearch(ElasticSearchInputVM input);
        //Task<ProductSearchOutputModel> ProductSearch(ProductElasticSearchInput input);

    }
}
