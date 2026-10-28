using EskaCMS.Engine.Elastic.BGS;
using EskaCMS.Engine.Elastic.Services.Interfaces;
using EskaCMS.Engine.MeiliSearch.Services.Intefaces;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Models.MeiliSearchVM;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.BlogArchive;
using Meilisearch;
using Microsoft.Extensions.Hosting;
using EskaCMS.Entities.Helpers;
using EskaCMS.Entities.model;

namespace EskaCMS.Engine.MeiliSearch.BGS
{
    public class MeiliSearchBackgroundService : BaseBackgroundService
    {
        public readonly ILogger<MeiliSearchBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private readonly System.IServiceProvider _serviceProvider;
       // private readonly MeilisearchClient client;

        public MeiliSearchBackgroundService(ILogger<MeiliSearchBackgroundService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
            : base(nameof(MeiliSearchBackgroundService), logger, serviceProvider, configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            Console.WriteLine("meili is created");
        }



        public async override Task<bool> Execute()
        {
            using (var scope = _serviceProvider.CreateScope())
            {

                try
                {
                    await AddUpdateNaviagtableSearchModels(scope);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"MeiliSearch Background Service {ex.ToString()}");

                }
                return true;
            }

        }

        #region Helper
        private async Task AddUpdateNaviagtableSearchModels(IServiceScope scope)
        {
            try
            {
                var allBrands = await GetBrandsNaviagtableModel(scope);
                var allProducts = await GetProductNaviagtableModel(scope);
                var allCategories = await GetCategoryNaviagtableModel(scope);
                var allPages = await GetPageNaviagtableModel(scope);
                var allPosts = await GetPostNaviagtableModel(scope);

                var meiliSearchService = scope.ServiceProvider.GetRequiredService<IMeiliSearchHelperService>();
                await meiliSearchService.IndexDocuments(EEntity.Page, allPages);
                await meiliSearchService.IndexDocuments(EEntity.Product, allProducts);
                await meiliSearchService.IndexDocuments(EEntity.Category, allCategories);
                await meiliSearchService.IndexDocuments(EEntity.Brand, allBrands);
                await meiliSearchService.IndexDocuments(EEntity.Post, allPosts);

                _logger.LogInformation("succsessfully sync data to meilisearch");
            }
            catch(Exception ex)
            {
                _logger.LogInformation($"Exception occurred {ex.Message}");
                throw;
            }


        }

        private async Task<List<NaviagtableMeiliSearchModel>> GetBrandsNaviagtableModel(IServiceScope scope)
        {
            try
            {
                var brandsList = await scope.ServiceProvider.GetRequiredService<IRepository<Brand>>().Query()
                       .AsNoTracking()
                       .Include(x => x.BrandDetails)
                       .ThenInclude(x => x.SlugInfo)
                       .Include(x => x.Thumbnail)
                       .Where(x => x.Status == EStatus.Published)
                       .ToListAsync();

                var brands = NavigatableSearchModelHelper.GetBrandsNaviagtableModel(brandsList);
                return brands;
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        private async Task<List<NaviagtableMeiliSearchModel>> GetProductNaviagtableModel(IServiceScope scope)
        {
            try
            {
                var productsList = await scope.ServiceProvider.GetRequiredService<IRepository<Product>>().Query()
                       .AsNoTracking()
                       .Include(x => x.ProductDetails)
                       .ThenInclude(x => x.SlugInfo)
                       .Include(x => x.ProductBranch)
                       .Include(x => x.ProductAvaliablity)
                       .Include(x => x.OptionCombinations)
                       .ThenInclude(x => x.Value)
                       .ThenInclude(x => x.productOptionValueDetails)
                       .Include(x => x.ThumbnailImage)
                       .Where(x => x.Status == EStatus.Published).ToListAsync();

                var products = NavigatableSearchModelHelper.GetProductNaviagtableModel(productsList);
                      

                return products;
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        private async Task<List<NaviagtableMeiliSearchModel>> GetCategoryNaviagtableModel(IServiceScope scope)
        {
            try
            {
                var categoryList = await scope.ServiceProvider.GetRequiredService<IRepository<Category>>().Query()
                        .AsNoTracking()
                        .Include(x => x.CategoryDetails)
                        .ThenInclude(x => x.SlugInfo)
                        .Include(x => x.ThumbnailImage)
                        .Where(x => x.Status == EStatus.Published).ToListAsync();

                var categories=NavigatableSearchModelHelper.GetCategoryNaviagtableModel(categoryList);


                return categories;
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        private async Task<List<NaviagtableMeiliSearchModel>> GetPageNaviagtableModel(IServiceScope scope)
        {
            try
            {
                var allPages = await GetAllPages(scope);
                var navigatablePages = NavigatableSearchModelHelper.GetPageNaviagtableModel(allPages);

                return navigatablePages;
            }
            catch (Exception ex)
            {

                throw;
            }



        }

        private async Task<List<NaviagtableMeiliSearchModel>> GetPostNaviagtableModel(IServiceScope scope)
        {
            try
            {
                var allpost = await GetAllPost(scope);
                var navigatablePost = NavigatableSearchModelHelper.GetPostNaviagtableModel(allpost);
                return navigatablePost;

            }
            catch (Exception ex)
            {

                throw;
            }


        }

        //Get pages with their cultures
        //private List<PageResultVM> BuildPagesTree(List<Page> pageList)
        //{
        //    List<Page> parentPages = new List<Page>();
        //    List<PageResultVM> pageResults = new List<PageResultVM>();

        //    parentPages = pageList.Where(x => x.ParentId == x.Id || x.ParentId == null).ToList();

        //    foreach (var page in parentPages)
        //    {
        //        PageResultVM pageResultVM = new PageResultVM();

        //        pageResultVM.Name = page.Name;
        //        pageResultVM.ParentPageId = page.Id;
        //        pageResultVM.ParentPageSlug = page.SlugInfo.Slug;
        //        pageResultVM.SiteId = page.SiteId;
        //        pageResultVM.PageCultureDetails = pageList.Where(x => x.ParentId == page.Id || x.Id == page.Id)
        //            .Select(c => new PageCultureDetailsVM()
        //            {
        //                Name = c.Name,
        //                PageId = c.Id,
        //                ParentPageId = c.ParentId,
        //                CultureCode = c.Culture.CultureId,
        //                CultureId = c.CultureId,
        //                Slug = c.SlugInfo.Slug,
        //                Description = c.Description,
        //                Keywords = c.Keywords
        //            })
        //            .ToList();


        //        pageResults.Add(pageResultVM);
        //    }

        //    return pageResults.ToList();
        //}
        private async Task<List<Page>> GetAllPages(IServiceScope scope)
        {
            try
            {
                var pagesRepo = scope.ServiceProvider.GetRequiredService<IRepository<Page>>();
                var pageList = await pagesRepo.Query()
                    .AsNoTracking()
                    .Include(x => x.Culture)
                    .Include(x => x.SlugInfo)
                    .Where(x => x.Status == EStatus.Published)
                    .ToListAsync();


                //var PageTree = NavigatableSearchModelHelper.BuildPagesTree(pageList);

                return pageList;



            }
            catch (Exception exc)
            {
                throw;
            }

        }

        public async Task<List<Posts>> GetAllPost(IServiceScope scope)
        {
            try
            {
                var postRepos = scope.ServiceProvider.GetRequiredService<IRepository<Posts>>();
                var query = await postRepos.Query()
                    .AsNoTracking()
                    .Include(p => p.SlugInfo)
                    .Include(p => p.Culture)
                    .Where(x => x.Status == EStatus.Published)
                   .ToListAsync();


                return query;
            }
            catch (Exception exc)
            {

                throw;
            }
        }
        //private List<PostsOutputVM> BuildPostsTree(List<Posts> AllPosts)
        //{
        //    try
        //    {
        //        List<PostsOutputVM> PostTree = new List<PostsOutputVM>();

        //        PostTree = AllPosts.Where(p => p.ParentId == p.Id).Select(p => new PostsOutputVM
        //        {
        //            Id = p.Id,
        //            Title = p.Title,
        //            ParentId = p.ParentId.Value,
        //            CultureId = p.CultureId,
        //            Slug = p.SlugInfo.Slug,
        //            SiteId = p.SiteId,
        //            PostCultures = AllPosts.Where(c => c.ParentId == p.Id).Select(c => new PostCulture()
        //            {
        //                Slug = c.SlugInfo.Slug,
        //                Id = c.Id,
        //                ParentId = c.ParentId.Value,
        //                Status = c.Status,
        //                Culture = c.Culture,
        //                Title = c.Title,
        //                Keywords = c.Keywords,
        //                Description = c.Description

        //            }).ToList()
        //        }).ToList();

        //        return PostTree;
        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }

        //}

        #endregion
    }
}