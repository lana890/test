
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Media.Services.Interfaces;
using EskaCMS.Search.Services.Interfaces;
using EskaCMS.Entities.Entity.Catalog;
using EskaCommerce.Catalog.Services;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Core.Areas.Core.ViewModels;
using MediatR;
using EskaCMS.Core.Events;
using EskaCMS.Entities.Entity.Search;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Search.Areas.Search.ViewModels;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Core.Services;
using Elastic.Clients.Elasticsearch;
using static MimeDetective.Definitions.Default.FileTypes;
using System.Threading;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.QueryDsl;
using EskaCMS.Infrastructure.Modules.Repositories;
using System.Reflection;
using AutoMapper.Internal;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.Infrastructure.Models.ElasticVM;
using EskaCMS.Infrastructure.Helpers;
using Humanizer;
using Meilisearch;
namespace EskaCMS.Search.Services
{
    class SearchService : ISearchService
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
        private readonly ISearchRepository<ProductElasticModel> _productSearchRepository;
        private readonly ElasticsearchClient _elasticsearchClient;
        private readonly MeilisearchClient _meilisearchClient;


        public SearchService(
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
            ISearchRepository<ProductElasticModel> productSearchRepository

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

        public async Task<SearchFilterListOutputVM> FullSearch(SearchFilterInputVM input)
        {
            List<SearchFilterOutputVM> finalOutput = new List<SearchFilterOutputVM>();

            long SiteId = await _workContext.GetCurrentSiteIdAsync();
            long CultureId = _workContext.GetCurrentCultureId();
            var userCountry = await userSiteService.GetUserCountry();
            var siteCountryId = await userSiteService.GetSiteCountry(SiteId);

            string inputwithTrim = string.IsNullOrWhiteSpace(input.SearchValue) ? null : input.SearchValue.ToLower().Trim();

            bool exactMatchesOnly = input.SubSearchOptions.Contains((int)SearchSubOptions.ExactMatchesOnly);

            bool searchInTitle = input.SubSearchOptions.Contains((int)SearchSubOptions.SearchInTitle);

            bool searchInDescription = input.SubSearchOptions.Contains((int)SearchSubOptions.SearchInDescription);

            bool searchInContent = input.SubSearchOptions.Contains((int)SearchSubOptions.SearchInContent);

            bool searchInAttribute = input.SubSearchOptions.Contains((int)SearchSubOptions.SearchInAttribute);

            if (input.SearchOptionId == (int)SearchOptions.SearchInAll || input.SearchOptionId == (int)SearchOptions.SearchInProducts || input.SearchOptionId == (int)SearchOptions.SearchInVariation)
            {
                List<SearchFilterOutputVM> productsOutput;
                productsOutput = await productDetailsRepository.Query()
                    .AsNoTracking()
                    .Include(c => c.Product)
                    .Include("Product.Categories")
                    .Include("Product.ProductPriceWarehouses")
                    .Include("Product.ProductBranch")
                    .Include("Product.LinkedProductLinks")
                    .Include("Product.ThumbnailImage")
                    .Include("Product.ProductLinks")
                    .Include("Product.ProductAvaliablity")
                    .Include("Product.ProductLinks.LinkedProduct")
                    .Include("Product.ProductLinks.LinkedProduct.ProductDetails")
                    .Include("Product.ProductLinks.LinkedProduct.ThumbnailImage")
                    .Include("Product.ProductLinks.LinkedProduct.ProductDetails.SlugInfo")
                    .Include("Product.AttributeValues")
                    .Include("Product.AttributeValues.Attribute")
                    .Include("Product.AttributeValues.AttributeValueDetails")
                    .Include("Product.AttributeValues.Attribute.AttributeDetails")
                    .Where(x => x.CultureId == CultureId && (x.Product.Status == EStatus.Published && x.Product.SiteId == SiteId))
                    .Include(x => x.SlugInfo)
                    .FilterProducts(input, exactMatchesOnly, searchInTitle, searchInDescription, searchInContent, searchInAttribute, input.SelectedAttrbuteId, inputwithTrim)
                    .Where(x => input.CategoryId == null || x.Product.Categories.Any(x => x.CategoryId == input.CategoryId))
                    .Select(x => new SearchFilterOutputVM
                    {
                        Title = x.Name,
                        Description = x.Description,
                        ThumbnailImageUrl = x.Product.ThumbnailImage == null ? "" : x.Product.ThumbnailImage.PublicUrl,
                        Slug = x.SlugInfo.Slug,

                        IsAvailable = IsAvaliableProduct(x.Product, userCountry, siteCountryId),
                        EType = EEntity.Product,
                        Id = x.ProductId,
                        MainProductId = x.Product.ProductLinks.Select(x => x.ProductId).FirstOrDefault(),
                        Attributes = !x.Product.AttributeValues.Any() ? new List<ItemDetailAttribute>() : x.Product.AttributeValues.Select(x => new ItemDetailAttribute
                        {
                            Id = x.Attribute.Id,
                            Name = x.Attribute == null ? "" : x.Attribute.AttributeDetails.Where(x => x.CultureId == CultureId).Select(x => x.Name).FirstOrDefault(),
                            Value = x.AttributeValueDetails.Count == 0 ? "" : x.AttributeValueDetails.Where(x => x.CultureId == CultureId).Select(x => x.Value).FirstOrDefault()
                        }).ToList()
                    }).ToListAsync();
                finalOutput.AddRange(productsOutput);
            }

            if (input.SearchOptionId == (int)SearchOptions.SearchInAll || input.SearchOptionId == (int)SearchOptions.SearchInPages)
            {

                List<SearchFilterOutputVM> PagesOutput;
                PagesOutput = await _pageRepository.Query()
                 .AsNoTrackingWithIdentityResolution()
                 .Include(x => x.PageVersions)
                 .Include(x => x.SlugInfo)
                 .Where(x => x.Status == EStatus.Published && x.SiteId == SiteId && x.CultureId == input.culture)
                 .FilterPage(input, exactMatchesOnly, searchInTitle, searchInDescription, searchInContent, inputwithTrim)
                 .Select(x => new SearchFilterOutputVM
                 {
                     Title = x.Name,
                     Description = x.Description,
                     ThumbnailId = x.ThumbnailId,
                     ThumbnailImageUrl = x.Thumbnail.PublicUrl,
                     Slug = x.SlugInfo.Slug,
                     EType = EEntity.Page

                 }).ToListAsync();
                finalOutput.AddRange(PagesOutput);


            }
            if (input.SearchOptionId == (int)SearchOptions.SearchInAll || input.SearchOptionId == (int)SearchOptions.SearchInPosts)
            {
                List<SearchFilterOutputVM> PostsOutput;


                PostsOutput = await _postRepository.Query()
                                .AsNoTrackingWithIdentityResolution()
                                .Include(x => x.SlugInfo)
                                .Include(x => x.PostMedias)
                                .Where(x => x.Status == EStatus.Published && x.SiteId == SiteId && x.CultureId == input.culture)
                                .FilterPosts(input, exactMatchesOnly, searchInTitle, searchInDescription, searchInContent, inputwithTrim)
                                                    .Select(x => new SearchFilterOutputVM
                                                    {
                                                        Title = x.Title,
                                                        Description = x.Description,
                                                        ThumbnailImageUrl = x.PostMedias.Select(y => y.Media.PublicUrl).FirstOrDefault(),
                                                        Slug = x.SlugInfo.Slug,
                                                        EType = EEntity.Post
                                                    }).ToListAsync();
                finalOutput.AddRange(PostsOutput);




            }

            await _mediator.Publish(new Core.Events.SearchQuery { SearchText = input.SearchValue, ResultCount = finalOutput.Count, SiteId = SiteId });

            return new SearchFilterListOutputVM
            {
                ListItems = finalOutput.Skip(input.PageSize * (input.PageNumber - 1))
                                .Take(input.PageSize).ToList(),
                TotalLength = finalOutput.Count
            };
        }


        public async Task<PaginationResult<MostKeywordsVM>> GetMostSearchKeywords(SmartTableParam model)
        {

            var siteId = _workContext.GetCurrentSiteId();

            var query = _queryRepository.Query()
                                          .Where(x => x.SiteId == siteId)
                                          .GroupBy(x => x.QueryText)
                                          .Select(g => new MostKeywordsVM { Keyword = g.Key, Count = g.Count() })
                                          .AsNoTracking();




            var result = await query.OrderByDescending(x => x.Count)
                                        .PaginateAsync(model.Pagination.Number, model.Pagination.Start);

            return result;


        }


        public async Task<bool> AddUserQuery(AddUserQueryVM model)
        {

            var siteId = _workContext.GetCurrentSiteId();

            var channel = _workContext.GetCurrentPlatform();

            var userId = await _workContext.GetCurrentUserId();


            var userQueryToBeAdded = new UserQueries
            {
                Api = model.Api,
                Payload = model.Payload,
                Channel = channel,
                Name = model.Name,
                SiteId = siteId,
                CreationDate = DateTimeOffset.Now,
                UserId = model.IsShared.HasValue && model.IsShared.Value ? null : userId,
                PageName = model.PageName
            };


            _userQueriesRepository.Add(userQueryToBeAdded);

            await _userQueriesRepository.SaveChangesAsync();
            return true;


        }


        public async Task<PaginationResult<GetUserQueriesDto>> GetUserQueries(SmartSearchTableParam<GetUserQueriesVM> model)
        {

            var siteId = _workContext.GetCurrentSiteId();


            var userId = await _workContext.GetCurrentUserId();


            var query = _userQueriesRepository.Query()
                                              .Where(x => x.SiteId == siteId && x.UserId == userId || x.UserId == null)
                                              .AsNoTracking();



            if (!string.IsNullOrEmpty(model.Search.Name))
            {
                string Name = model.Search.Name.ToUpper();
                query = query.Where(x => x.Name.ToUpper().Contains(Name));
            }
            if (!string.IsNullOrEmpty(model.Search.Api))
            {
                string Api = model.Search.Api.ToUpper();
                query = query.Where(x => x.Api.ToUpper().Contains(Api));
            }

            if (!string.IsNullOrEmpty(model.Search.PageName))
            {
                string PageName = model.Search.PageName.ToUpper();
                query = query.Where(x => x.PageName.ToUpper().Contains(PageName));
            }
            if (model.Search.IsShared.HasValue)
            {
                bool IsShared = model.Search.IsShared.Value;


                query = query.Where(x => IsShared ? x.UserId == null : true);
            }
            if (model.Search.Source.HasValue)
            {
                EPlatformSource Source = model.Search.Source.Value;
                query = query.Where(x => x.Channel == Source);
            }

            var result = await query.Select(x => new GetUserQueriesDto
            {
                Id = x.Id,
                Api = x.Api,
                Source = x.Channel,
                Name = x.Name,
                Payload = x.Payload,
                IsShared = x.UserId.HasValue ? false : true,
                PageName = x.PageName
            }).PaginateAsync(model.Pagination.Number, model.Pagination.Start);



            return result;


        }
        private static bool IsAvaliableProduct(Product product, UserCountryVM userCountry, string siteCountryId)
        {
            var AllowdProductBaseedOnPrice = userCountry.IsRegional ? userCountry.IsRegional : siteCountryId == userCountry.Id ? true : product.ForeignPrice.HasValue;

            return (AllowdProductBaseedOnPrice && (product.ProductAvaliablity.Count() == 0  || product.ProductAvaliablity.Any(i => i.CountryId == userCountry.Id && i.Permission == EProductCountryPermission.Allowed)));
        }

        public async Task<bool> DeleteUserQueries(long queryId)
        {

            var siteId = _workContext.GetCurrentSiteId();


            var userId = await _workContext.GetCurrentUserId();


            var query = await _userQueriesRepository.Query()
                                              .Where(x => x.Id == queryId && x.SiteId == siteId)
                                              .AsNoTracking()
                                              .FirstOrDefaultAsync();


            if (query is null)
            {
                throw new Exception("User query doesn't exist");
            }

            if (query.UserId != userId)
            {
                throw new Exception("You don't have permession to remove this query");
            }

            _userQueriesRepository.Remove(query);

            await _userQueriesRepository.SaveChangesAsync();



            return true;


        }

        public async Task<NaviagtableSearchOutputModel> ElasticFullSearch(ElasticSearchInputVM input)
        {
            
            if (string.IsNullOrEmpty(input.SearchValue))
            { 
                return new  NaviagtableSearchOutputModel { ListItems = new List<Details>(), TotalCount = 0 };
            }
            long siteId = _workContext.GetCurrentSiteId();
            long warehouseId = await _workContext.GetCurrentWarehouseId();
            var UserCountryId =await userSiteService.GetUserCountry();
            long CultureId = _workContext.GetCurrentCultureId();
            var searchRequestConfig = new SearchRequestConfig();
            searchRequestConfig.PageSize= input.PageSize;
            searchRequestConfig.PageNumber = input.PageNumber;
            searchRequestConfig.Fuziness = input.Fuziness;

            searchRequestConfig.SiteId = siteId;
            foreach (var type in input.EntityTypes)
            {
                if (System.Enum.IsDefined(typeof(EEntity), type))
                {
                    var stringType = type.GetDisplayName();
                    if (stringType is not null && !string.IsNullOrEmpty(stringType)) 
                    {
                        searchRequestConfig.Options.Add(new KeyValuePair<string, object>( "entityType.keyword", stringType));
                    }
                }
            }
            if (searchRequestConfig.SiteId.HasValue && searchRequestConfig.SiteId !=0)
            {
                searchRequestConfig.Options.Add(new KeyValuePair<string, object>("siteId",searchRequestConfig.SiteId));
            }

            var elasticRes = await _searchRepository.FullTextSearch(input.SearchValue, searchRequestConfig);


            var result = elasticRes.Result.SelectMany(x=>x.Details).Where(p=>p.CultureId==CultureId).DistinctBy(x=>x.Slug).ToList();

            if (result.Any())
            {
                result = result.Where(x => (string.IsNullOrEmpty(x.BranchId) || x.BranchId.Contains(warehouseId.ToString())))
                               .Where(x => (string.IsNullOrEmpty(x.CountryId) || x.CountryId.Contains(UserCountryId.Id)))
                               .ToList();

            }
            
            await _mediator.Publish(new Core.Events.SearchQuery { SearchText = input.SearchValue, ResultCount =result.Count, SiteId = siteId });

            return new NaviagtableSearchOutputModel { ListItems = result , TotalCount = result.Count };

        }


        public async Task<ProductSearchOutputModel> ProductSearch(ProductElasticSearchInput input)
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

