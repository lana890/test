//using EskaCMS.Core.Data;
//using static EskaCMS.Entities.Enum.GeneralEnums;
//using EskaCMS.Core.Extensions;
//using EskaCMS.Core.Models;
//using EskaCMS.Core.Services;
//using EskaCMS.Core.Services.Interface;
//using EskaCMS.Infrastructure.Data;

//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//using EskaCMS.Infrastructure.Models.ElasticVM;


//namespace EskaCMS.Infrastructure.Helpers
//{
//    // Infrastructure/Helpers/NavigatableSearchModelHelper.cs
//    public static class NavigatableSearchModelHelper
//    {
//        public static async Task<List<NaviagtableSearchModel>> GetBrandsNavigatableModel(
//            IRepository<T> brandRepository)
//        {
//            try
//            {
//                var brands = await brandRepository.Query()
//                    .AsNoTracking()
//                    .Include(x => x.BrandDetails)
//                .ThenInclude(x => x.SlugInfo)
//                    .Include(x => x.Thumbnail)
//                    .Where(x => x.Status == EStatus.Published)
//                    .Select(x => new NaviagtableSearchModel()
//                    {
//                        Id = x.Id,
//                        ElasticId = $"{x.Id}-{nameof(EEntity.Brand)}",
//                        EntityType = $"{nameof(EEntity.Brand)}",
//                        SiteId = x.SiteId,
//                        Details = x.BrandDetails.Select(bd => new Details()
//                        {
//                            Name = bd.Name,
//                            Description = bd.Description,
//                            Slug = bd.SlugInfo.Slug,
//                            CultureId = bd.CultureId,
//                            addtionalMetaTags = string.Empty,
//                            Keywords = string.Empty,
//                            MetaDescription = string.Empty,
//                            ShortDescription = string.Empty,
//                            NormalizedName = string.Empty,
//                            Specification = string.Empty,
//                            ThumbnailImageUrl = x.Thumbnail == null ? "" : x.Thumbnail.PublicUrl,
//                        }).ToList()
//                    }).ToListAsync();

//                return brands;
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        public static async Task<List<NaviagtableSearchModel>> GetProductNavigatableModel(
//            IRepository<Product> productRepository)
//        {
//            try
//            {
//                var products = await productRepository.Query()
//                    .AsNoTracking()
//                    .Include(x => x.ProductDetails)
//                    .ThenInclude(x => x.SlugInfo)
//                    .Include(x => x.ProductBranch)
//                    .Include(x => x.ProductAvaliablity)
//                    .Include(x => x.OptionCombinations)
//                    .ThenInclude(x => x.Value)
//                .ThenInclude(x => x.productOptionValueDetails)
//                    .Include(x => x.ThumbnailImage)
//                    .Where(x => x.Status == EStatus.Published)
//                    .Select(x => new NaviagtableSearchModel()
//                    {
//                        Id = x.Id,
//                        ElasticId = $"{x.Id}-{nameof(EEntity.Product)}",
//                        EntityType = $"{nameof(EEntity.Product)}",
//                        SiteId = x.SiteId,
//                        Details = x.ProductDetails.Select(bd => new Details()
//                        {
//                            Name = bd.Name + (x.OptionCombinations.Any() ?
//                                " " + string.Join("_", x.OptionCombinations
//                                    .SelectMany(oc => oc.Value.productOptionValueDetails)
//                                    .Where(d => d.CultureId == bd.CultureId)
//                                    .Select(d => d.Value).ToList()) : ""),
//                            Description = bd.Description,
//                            Slug = bd.SlugInfo.Slug,
//                            CultureId = bd.CultureId,
//                            addtionalMetaTags = bd.AddtionalMetaTags,
//                            Keywords = string.Empty,
//                            MetaDescription = string.Empty,
//                            ShortDescription = bd.ShortDescription,
//                            NormalizedName = bd.NormalizedName,
//                            Specification = bd.Specification,
//                            ThumbnailImageUrl = x.ThumbnailImage == null ? "" : x.ThumbnailImage.PublicUrl,
//                            BranchId = x.ProductBranch.Any() ?
//                                string.Join(",", x.ProductBranch.Select(pb => pb.WarehouseId).ToList()) : "",
//                            CountryId = x.ProductAvaliablity.Any() ?
//                                string.Join(",", x.ProductAvaliablity.Select(pa => pa.CountryId).ToList()) : "",
//                            Id = bd.ProductId
//                        }).ToList()
//                    }).ToListAsync();

//                return products;
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        public static async Task<List<NaviagtableSearchModel>> GetCategoryNavigatableModel(
//            IRepository<Category> categoryRepository)
//        {
//            try
//            {
//                var categories = await categoryRepository.Query()
//                    .AsNoTracking()
//                    .Include(x => x.CategoryDetails)
//                .ThenInclude(x => x.SlugInfo)
//                    .Include(x => x.ThumbnailImage)
//                    .Where(x => x.Status == EStatus.Published)
//                    .Select(x => new NaviagtableSearchModel()
//                    {
//                        Id = x.Id,
//                        ElasticId = $"{x.Id}-{nameof(EEntity.Category)}",
//                        EntityType = $"{nameof(EEntity.Category)}",
//                        SiteId = x.SiteId ?? 0,
//                        Details = x.CategoryDetails.Select(bd => new Details()
//                        {
//                            Name = bd.Name,
//                            Description = bd.Description,
//                            Slug = bd.SlugInfo.Slug ?? string.Empty,
//                            CultureId = bd.CultureId ?? 0,
//                            addtionalMetaTags = string.Empty,
//                            Keywords = bd.MetaKeywords,
//                            MetaDescription = bd.MetaDescription,
//                            ShortDescription = string.Empty,
//                            NormalizedName = string.Empty,
//                            Specification = string.Empty,
//                            ThumbnailImageUrl = x.ThumbnailImage == null ? "" : x.ThumbnailImage.PublicUrl,
//                        }).ToList()
//                    }).ToListAsync();

//                return categories;
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        public static async Task<List<NaviagtableSearchModel>> GetPageNavigatableModel(
//            IRepository<Page> pageRepository)
//        {
//            try
//            {
//                var pageList = await pageRepository.Query()
//                    .AsNoTracking()
//                .Include(x => x.Culture)
//                    .Include(x => x.SlugInfo)
//                    .Where(x => x.Status == EStatus.Published)
//                    .ToListAsync();

//                var pageTree = BuildPagesTree(pageList);

//                var navigatablePages = pageTree.Select(p => new NaviagtableSearchModel()
//                {
//                    Id = p.ParentPageId ?? 0,
//                    ElasticId = $"{p.ParentPageId ?? 0}-{nameof(EEntity.Page)}",
//                    EntityType = $"{nameof(EEntity.Page)}",
//                    SiteId = p.SiteId,
//                    Details = p.PageCultureDetails.Select(pc => new Details()
//                    {
//                        Name = pc.Name,
//                        Description = pc.Description,
//                        Slug = pc.Slug,
//                        CultureId = pc.CultureId,
//                        addtionalMetaTags = string.Empty,
//                        Keywords = pc.Keywords,
//                        MetaDescription = string.Empty,
//                        ShortDescription = string.Empty,
//                        NormalizedName = string.Empty,
//                        Specification = string.Empty,
//                        ThumbnailImageUrl = string.Empty,
//                    }).ToList()
//                }).ToList();

//                return navigatablePages;
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        public static async Task<List<NaviagtableSearchModel>> GetPostNavigatableModel(
//            IRepository<Posts> postRepository)
//        {
//            try
//            {
//                var allPosts = await postRepository.Query()
//                    .AsNoTracking()
//                .Include(p => p.SlugInfo)
//                    .Include(p => p.Culture)
//                    .Where(x => x.Status == EStatus.Published)
//                    .ToListAsync();

//                var postTree = BuildPostsTree(allPosts);

//                var navigatablePosts = postTree.Select(p => new NaviagtableSearchModel()
//                {
//                    Id = p.Id,
//                    ElasticId = $"{p.Id}-{nameof(EEntity.Post)}",
//                    EntityType = $"{nameof(EEntity.Post)}",
//                    SiteId = p.SiteId,
//                    Details = p.PostCultures.Select(pc => new Details()
//                    {
//                        Name = pc.Title,
//                        Description = pc.Description,
//                        Slug = pc.Slug,
//                        CultureId = pc.Culture.Id,
//                        addtionalMetaTags = string.Empty,
//                        Keywords = pc.Keywords,
//                        MetaDescription = string.Empty,
//                        ShortDescription = string.Empty,
//                        NormalizedName = string.Empty,
//                        Specification = string.Empty,
//                        ThumbnailImageUrl = string.Empty,
//                    }).ToList()
//                }).ToList();

//                return navigatablePosts;
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        #region Private Helper Methods

//        private static List<PageResultVM> BuildPagesTree(List<Page> pageList)
//        {
//            List<Page> parentPages = pageList.Where(x => x.ParentId == x.Id || x.ParentId == null).ToList();
//            List<PageResultVM> pageResults = new List<PageResultVM>();

//            foreach (var page in parentPages)
//            {
//                PageResultVM pageResultVM = new PageResultVM
//                {
//                    Name = page.Name,
//                    ParentPageId = page.Id,
//                    ParentPageSlug = page.SlugInfo.Slug,
//                    SiteId = page.SiteId,
//                    PageCultureDetails = pageList.Where(x => x.ParentId == page.Id || x.Id == page.Id)
//                        .Select(c => new PageCultureDetailsVM()
//                        {
//                            Name = c.Name,
//                            PageId = c.Id,
//                            ParentPageId = c.ParentId,
//                            CultureCode = c.Culture.CultureId,
//                            CultureId = c.CultureId,
//                            Slug = c.SlugInfo.Slug,
//                            Description = c.Description,
//                            Keywords = c.Keywords
//                        }).ToList()
//                };

//                pageResults.Add(pageResultVM);
//            }

//            return pageResults;
//        }

//        private static List<PostsOutputVM> BuildPostsTree(List<Posts> allPosts)
//        {
//            try
//            {
//                List<PostsOutputVM> postTree = allPosts.Where(p => p.ParentId == p.Id).Select(p => new PostsOutputVM
//                {
//                    Id = p.Id,
//                    Title = p.Title,
//                    ParentId = p.ParentId.Value,
//                    CultureId = p.CultureId,
//                    Slug = p.SlugInfo.Slug,
//                    SiteId = p.SiteId,
//                    PostCultures = allPosts.Where(c => c.ParentId == p.Id).Select(c => new PostCulture()
//                    {
//                        Slug = c.SlugInfo.Slug,
//                        Id = c.Id,
//                        ParentId = c.ParentId.Value,
//                        Status = c.Status,
//                        Culture = c.Culture,
//                        Title = c.Title,
//                        Keywords = c.Keywords,
//                        Description = c.Description
//                    }).ToList()
//                }).ToList();

//                return postTree;
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        #endregion
//    }
//}
