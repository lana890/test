
using EskaCMS.BlogArchive.Models;
using EskaCMS.Engine.Elastic.Services.Interfaces;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Models.ElasticVM;
using EskaCMS.Pages.Models;
using Microsoft.EntityFrameworkCore;
using static EskaCMS.Entities.Enum.GeneralEnums;
namespace EskaCMS.Engine.Elastic.BGS;


public class ElasticSearchBackgroundService : BaseBackgroundService
{
    public readonly ILogger<ElasticSearchBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private readonly System.IServiceProvider _serviceProvider;

    public ElasticSearchBackgroundService(ILogger<ElasticSearchBackgroundService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        : base(nameof(ElasticSearchBackgroundService), logger, serviceProvider, configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
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
                _logger.LogInformation($"Elastic Search Background Service {ex.ToString()}");

            }
            return true;
        }

    }

    #region Helper
    private async Task AddUpdateNaviagtableSearchModels(IServiceScope scope)
    {
        var allBrands = await GetBrandsNaviagtableModel(scope);
        var allProducts = await GetProductNaviagtableModel(scope);
        var allCategories = await GetCategoryNaviagtableModel(scope);
        var allPages = await GetPageNaviagtableModel(scope);
        var allPosts = await GetPostNaviagtableModel(scope);

        var allNaviagtableSearchModel = new List<NaviagtableSearchModel>();
        allNaviagtableSearchModel.AddRange(allBrands);
        allNaviagtableSearchModel.AddRange(allProducts);
        allNaviagtableSearchModel.AddRange(allCategories);
        allNaviagtableSearchModel.AddRange(allPages);
        allNaviagtableSearchModel.AddRange(allPosts);

        var elasticService = scope.ServiceProvider.GetRequiredService<IElasticHelperService>();
        await elasticService.AddUpdateBulk(allNaviagtableSearchModel);




    }

    private async Task<List<NaviagtableSearchModel>> GetBrandsNaviagtableModel(IServiceScope scope)
    {
        try
        {
            var brands = await scope.ServiceProvider.GetRequiredService<IRepository<Brand>>().Query()
                   .AsNoTracking()
                   .Include(x => x.BrandDetails)
                   .ThenInclude(x => x.SlugInfo)
                   .Include(x => x.Thumbnail)
                   .Where(x => x.Status == EStatus.Published)
                   .Select(x => new NaviagtableSearchModel()
                   {
                       Id = x.Id,
                       ElasticId = $"{x.Id}-{nameof(EEntity.Brand)}",
                       EntityType = $"{nameof(EEntity.Brand)}",
                       SiteId = x.SiteId,
                       Details = x.BrandDetails.Select(bd => new Details()
                       {
                           Name = bd.Name,
                           Description = bd.Description,
                           Slug = bd.SlugInfo.Slug,
                           CultureId = bd.CultureId,
                           addtionalMetaTags = string.Empty,
                           Keywords = string.Empty,
                           MetaDescription = string.Empty,
                           ShortDescription = string.Empty,
                           NormalizedName = string.Empty,
                           Specification = string.Empty,
                           ThumbnailImageUrl = x.Thumbnail == null ? "" : x.Thumbnail.PublicUrl,

                       }
                      ).ToList()

                   }).ToListAsync();

            return brands;
        }
        catch (Exception ex)
        {

            throw;
        }

    }

    private async Task<List<NaviagtableSearchModel>> GetProductNaviagtableModel(IServiceScope scope)
    {
        try
        {
            var products = await scope.ServiceProvider.GetRequiredService<IRepository<Product>>().Query()
                   .AsNoTracking()
                   .Include(x => x.ProductDetails)
                   .ThenInclude(x => x.SlugInfo)
                   .Include(x=>x.ProductBranch)
                   .Include(x=>x.ProductAvaliablity)
                   .Include(x => x.OptionCombinations)
                   .ThenInclude(x => x.Value)
                   .ThenInclude(x => x.productOptionValueDetails)
                   .Include(x => x.ThumbnailImage)
                   .Where(x => x.Status == EStatus.Published)
                   .Select(x => new NaviagtableSearchModel()
                   {
                       Id = x.Id,
                       ElasticId = $"{x.Id}-{nameof(EEntity.Product)}",
                       EntityType = $"{nameof(EEntity.Product)}",
                       SiteId = x.SiteId,
                       Details = x.ProductDetails.Select(bd => new Details()
                       {
                           Name = bd.Name + (x.OptionCombinations.Any()? " " + string.Join("_",x.OptionCombinations.SelectMany(x=>x.Value.productOptionValueDetails).Where(x=>x.CultureId==bd.CultureId).Select(x=>x.Value).ToList()):""),
                           Description = bd.Description,
                           Slug = bd.SlugInfo.Slug,
                           CultureId = bd.CultureId,
                           addtionalMetaTags = bd.AddtionalMetaTags,
                           Keywords = string.Empty,
                           MetaDescription = string.Empty,
                           ShortDescription = bd.ShortDescription,
                           NormalizedName = bd.NormalizedName,
                           Specification = bd.Specification,
                           ThumbnailImageUrl = x.ThumbnailImage == null ? "" : x.ThumbnailImage.PublicUrl,
                           BranchId = x.ProductBranch.Any() ? string.Join(",", x.ProductBranch.Select(x => x.WarehouseId).ToList()) : "",
                           CountryId = x.ProductAvaliablity.Any() ? string.Join(",", x.ProductAvaliablity.Select(x => x.CountryId).ToList()) : "",
                           Id = bd.ProductId

                       }
                      ).ToList()

                   }).ToListAsync();

            return products;
        }
        catch (Exception ex)
        {

            throw;
        }

    }

    private async Task<List<NaviagtableSearchModel>> GetCategoryNaviagtableModel(IServiceScope scope)
    {
        try
        {
            var categories = await scope.ServiceProvider.GetRequiredService<IRepository<Category>>().Query()
                    .AsNoTracking()
                    .Include(x => x.CategoryDetails)
                    .ThenInclude(x => x.SlugInfo)
                    .Include(x => x.ThumbnailImage)
                    .Where(x => x.Status == EStatus.Published)
                    .Select(x => new NaviagtableSearchModel()
                    {
                        Id = x.Id,
                        ElasticId = $"{x.Id}-{nameof(EEntity.Category)}",
                        EntityType = $"{nameof(EEntity.Category)}",
                        SiteId = x.SiteId ?? 0,
                        Details = x.CategoryDetails.Select(bd => new Details()
                        {

                            Name = bd.Name,
                            Description = bd.Description,
                            Slug = bd.SlugInfo.Slug ?? string.Empty,
                            CultureId = bd.CultureId ?? 0,
                            addtionalMetaTags = string.Empty,
                            Keywords = bd.MetaKeywords,
                            MetaDescription = bd.MetaDescription,
                            ShortDescription = string.Empty,
                            NormalizedName = string.Empty,
                            Specification = string.Empty,
                            ThumbnailImageUrl = x.ThumbnailImage == null ? "" : x.ThumbnailImage.PublicUrl,


                        }
                       ).ToList()

                    }).ToListAsync();

            return categories;
        }
        catch (Exception ex)
        {

            throw ;
        }

    }

    private async Task<List<NaviagtableSearchModel>> GetPageNaviagtableModel(IServiceScope scope)
    {
        try
        {
            var allPages = await GetAllPages(scope);
            var navigatablePages = allPages.Select(p => new NaviagtableSearchModel()
            {
                Id = p.ParentPageId ?? 0,
                ElasticId = $"{p.ParentPageId ?? 0}-{nameof(EEntity.Page)}",
                EntityType = $"{nameof(EEntity.Page)}",
                SiteId = p.SiteId,
                Details = p.PageCultureDetails.Select(pc => new Details()
                {
                    Name = pc.Name,
                    Description = pc.Description,
                    Slug = pc.Slug,
                    CultureId = pc.CultureId,
                    addtionalMetaTags = string.Empty,
                    Keywords = pc.Keywords,
                    MetaDescription = string.Empty,
                    ShortDescription = string.Empty,
                    NormalizedName = string.Empty,
                    Specification = string.Empty,
                    ThumbnailImageUrl = string.Empty,
                }).ToList()

            }).ToList();

            return navigatablePages;
        }
        catch (Exception ex)
        {

            throw;
        }



    }

    private async Task<List<NaviagtableSearchModel>> GetPostNaviagtableModel(IServiceScope scope)
    {
        try
        {
            var allpost = await GetAllPost(scope);
            var navigatablePost = allpost.Select(p => new NaviagtableSearchModel()
            {
                Id = p.Id,
                ElasticId = $"{p.Id}-{nameof(EEntity.Post)}",
                EntityType = $"{nameof(EEntity.Post)}",
                SiteId = p.SiteId,
                Details = p.PostCultures.Select(pc => new Details()
                {
                    Name = pc.Title,
                    Description = pc.Description,
                    Slug = pc.Slug,
                    CultureId = pc.Culture.Id,
                    addtionalMetaTags = string.Empty,
                    Keywords = pc.Keywords,
                    MetaDescription = string.Empty,
                    ShortDescription = string.Empty,
                    NormalizedName = string.Empty,
                    Specification = string.Empty,
                    ThumbnailImageUrl = string.Empty,

                }).ToList()

            }).ToList();

            return navigatablePost;

        }
        catch (Exception ex)
        {

            throw;
        }


    }

    private async Task<List<PageResultVM>> GetAllPages(IServiceScope scope)
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


            var PageTree = BuildPagesTree(pageList);

            return PageTree;



        }
        catch (Exception exc)
        {
            throw ;
        }

    }

    private List<PageResultVM> BuildPagesTree(List<Page> pageList)
    {
        List<Page> parentPages = new List<Page>();
        List<PageResultVM> pageResults = new List<PageResultVM>();

        parentPages = pageList.Where(x => x.ParentId == x.Id || x.ParentId == null).ToList();

        foreach (var page in parentPages)
        {
            PageResultVM pageResultVM = new PageResultVM();

            pageResultVM.Name = page.Name;
            pageResultVM.ParentPageId = page.Id;
            pageResultVM.ParentPageSlug = page.SlugInfo.Slug;
            pageResultVM.SiteId = page.SiteId;
            pageResultVM.PageCultureDetails = pageList.Where(x => x.ParentId == page.Id || x.Id == page.Id)
                .Select(c => new PageCultureDetailsVM()
                {
                    Name = c.Name,
                    PageId = c.Id,
                    ParentPageId = c.ParentId,
                    CultureCode = c.Culture.CultureId,
                    CultureId = c.CultureId,
                    Slug = c.SlugInfo.Slug,
                    Description = c.Description,
                    Keywords = c.Keywords
                })
                .ToList();


            pageResults.Add(pageResultVM);
        }

        return pageResults.ToList();
    }

    public async Task<List<PostsOutputVM>> GetAllPost(IServiceScope scope)
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


            return BuildPostsTree(query);
        }
        catch (Exception exc)
        {

            throw;
        }
    }

    private List<PostsOutputVM> BuildPostsTree(List<Posts> AllPosts)
    {
        try
        {
            List<PostsOutputVM> PostTree = new List<PostsOutputVM>();

            PostTree = AllPosts.Where(p => p.ParentId == p.Id).Select(p => new PostsOutputVM
            {
                Id = p.Id,
                Title = p.Title,
                ParentId = p.ParentId.Value,
                CultureId = p.CultureId,
                Slug = p.SlugInfo.Slug,
                SiteId = p.SiteId,
                PostCultures = AllPosts.Where(c => c.ParentId == p.Id).Select(c => new PostCulture()
                {
                    Slug = c.SlugInfo.Slug,
                    Id = c.Id,
                    ParentId = c.ParentId.Value,
                    Status = c.Status,
                    Culture = c.Culture,
                    Title = c.Title,
                    Keywords = c.Keywords,
                    Description = c.Description


                }).ToList()
            }).ToList();

            return PostTree;
        }
        catch (Exception ex)
        {

            throw;
        }

    }

    #endregion

}