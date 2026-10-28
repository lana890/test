using Elastic.Clients.Elasticsearch;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Models.MeiliSearchVM;
using EskaCMS.Infrastructure.Modules.Repositories;
using EskaCMS.Search.Model;
using EskaCMS.Search.Services.Interfaces;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using MediatR;
using Meilisearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Search.Services
{
    public class MeiliSearchServices:IMeiliSearchService
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductDetails> productDetailsRepository;
        private readonly IWorkContext _workContext;
        private readonly IUserSiteService userSiteService;
        private readonly IRepository<Page> _pageRepository;
        private readonly IRepository<Posts> _postRepository;
        private readonly IMediator _mediator;
        private readonly IRepository<Entities.Entity.Search.Query> _queryRepository;
        private readonly IRepository<UserQueries> _userQueriesRepository;
        private readonly ISearchRepository<NaviagtableSearchModel> _searchRepository;
        private readonly ISearchRepository<ProductMeiliModel> _productSearchRepository;
        private readonly MeilisearchClient _meilisearchClient;


        public MeiliSearchServices(
            IRepository<Product> productRepository,
            IRepository<ProductDetails> productDetailsRepository,
            IRepository<Page> pageRepository,
            IRepository<Posts> postRepository,
            IWorkContext workContext,
            IUserSiteService userSiteService,
            IMediator mediator,
            IRepository<Entities.Entity.Search.Query> queryRepository,
            IRepository<UserQueries> userQueriesRepository,
            ISearchRepository<NaviagtableSearchModel> searchRepository,
            ISearchRepository<ProductMeiliModel> productSearchRepository

                   )
        {
            _productRepository = productRepository;
            this.productDetailsRepository = productDetailsRepository;
            _postRepository = postRepository;
            _pageRepository = pageRepository;
            _workContext = workContext;
            this.userSiteService = userSiteService;
            _mediator = mediator;
            _queryRepository = queryRepository;
            _userQueriesRepository = userQueriesRepository;
            _searchRepository = searchRepository;
            _productSearchRepository = productSearchRepository;

        }



        public async Task<NaviagtableSearchOutputModel> MeiliFullSearch(MeiliSearchInputVM input)
        {

            if (string.IsNullOrEmpty(input.SearchValue))
            {
                return new NaviagtableSearchOutputModel { ListItems = new List<Infrastructure.Models.MeiliSearchVM.Details>(), TotalCount = 0 };
            }
            long siteId = _workContext.GetCurrentSiteId();
            long warehouseId = await _workContext.GetCurrentWarehouseId();
            var UserCountryId = await userSiteService.GetUserCountry();
            long CultureId = _workContext.GetCurrentCultureId();
            var searchRequestConfig = new SearchRequestConfig();
            searchRequestConfig.PageSize = input.PageSize;
            searchRequestConfig.PageNumber = input.PageNumber;

            searchRequestConfig.SiteId = siteId;
            
            foreach (var type in input.EntityTypes)
            {
                if (System.Enum.IsDefined(typeof(EEntity), type))
                {
                    var stringType = type.GetDisplayName();
                    if (stringType is not null && !string.IsNullOrEmpty(stringType))
                    {
                        searchRequestConfig.Options.Add(new KeyValuePair<string, object>("entityType.keyword", stringType));
                    }
                    searchRequestConfig.EntityType = type.GetDisplayName();
                }
            }
            if (searchRequestConfig.SiteId.HasValue && searchRequestConfig.SiteId != 0)
            {
                searchRequestConfig.Options.Add(new KeyValuePair<string, object>("siteId", searchRequestConfig.SiteId));
            }

            var meiliRes = await _searchRepository.FullTextSearch(input.SearchValue, searchRequestConfig);


            List<Infrastructure.Models.MeiliSearchVM.Details> result = meiliRes.Result.SelectMany(x => x.Details).Where(p => p.CultureId == CultureId).DistinctBy(x => x.Slug).ToList();

            if (result.Any())
            {
                result = result.Where(x => (string.IsNullOrEmpty(x.BranchId) || x.BranchId.Contains(warehouseId.ToString())))
                               .Where(x => (string.IsNullOrEmpty(x.CountryId) || x.CountryId.Contains(UserCountryId.Id)))
                               .ToList();

            }

            await _mediator.Publish(new Core.Events.SearchQuery { SearchText = input.SearchValue, ResultCount = result.Count, SiteId = siteId });

            return new NaviagtableSearchOutputModel { ListItems = result, TotalCount = result.Count };

        }


        public async Task<ProductSearchOutputModel> ProductSearch(ProductMeiliSearchInput input)
        {

            long siteId = _workContext.GetCurrentSiteId();
            long CultureId = _workContext.GetCurrentCultureId();
            var searchRequestConfig = new ProductSearchRequestConfig();
            searchRequestConfig.PageSize = input.PageSize;
            searchRequestConfig.PageNumber = input.PageNumber;
            searchRequestConfig.SiteId = siteId;
            searchRequestConfig.CategoriesIds = input.CategoriesIds;
            searchRequestConfig.CultureId = CultureId;
            if (searchRequestConfig.SiteId.HasValue && searchRequestConfig.SiteId != 0)
            {
                searchRequestConfig.Options.Add(new KeyValuePair<string, object>("siteId", searchRequestConfig.SiteId));
            }
            if (searchRequestConfig.CultureId.HasValue && searchRequestConfig.CultureId != 0)
            {
                searchRequestConfig.Options.Add(new KeyValuePair<string, object>("cultureId", searchRequestConfig.CultureId));
            }
            if (searchRequestConfig.CategoriesIds != null)
            {
                foreach (var cat in searchRequestConfig.CategoriesIds)
                {
                    searchRequestConfig.Options.Add(new KeyValuePair<string, object>("categoryDetails.categoryId", cat));

                }
            }
            var elasticRes = await _productSearchRepository.ProductSearch(input.SearchValue, searchRequestConfig);

            var result = elasticRes.Result.ToList();

            await _mediator.Publish(new Core.Events.SearchQuery { SearchText = input.SearchValue, ResultCount = result.Count, SiteId = siteId });

            return new ProductSearchOutputModel { ListItems = result, TotalCount = result.Count };
        }
    }
}
