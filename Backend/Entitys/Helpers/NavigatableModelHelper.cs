using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.model;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Models.MeiliSearchVM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml.Drawing.Slicer.Style;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Helpers
{
    public static class NavigatableSearchModelHelper
    {
        public static List<NaviagtableMeiliSearchModel> GetBrandsNaviagtableModel(dynamic brand)
        {
            try
            {
                List<Brand> brandsList;
                if (brand is List<Brand>)
                {
                    brandsList = brand;
                }
                else
                {
                    brandsList = new List<Brand> { brand };
                }
                var brands = brandsList
                       .Select(x => new NaviagtableMeiliSearchModel()
                       {
                           Id = x.Id,
                           MeiliId = $"{x.Id}-{nameof(EEntity.Brand)}",
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

                           }).ToList()

                       }).ToList();

                return brands;
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public static  List<NaviagtableMeiliSearchModel> GetProductNaviagtableModel(dynamic prod)
        {
            try
            {
                List<Product>ProductsList;
                if (prod is List<Product>)
                {
                    ProductsList = prod;
                }
                else
                {
                    ProductsList = new List<Product> { prod };
                }
                var products = ProductsList
                       .Select(x => new NaviagtableMeiliSearchModel()
                       {
                           Id = x.Id,
                           MeiliId = $"{x.Id}-{nameof(EEntity.Product)}",
                           EntityType = $"{nameof(EEntity.Product)}",
                           SiteId = x.SiteId,
                           Details = x.ProductDetails.Select(bd => new Details()
                           {
                               Name = bd.Name + (x.OptionCombinations.Any() ? " " + string.Join("_", x.OptionCombinations.SelectMany(x => x.Value.productOptionValueDetails).Where(x => x.CultureId == bd.CultureId).Select(x => x.Value).ToList()) : ""),
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

                       }).ToList();

                return products;
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public static List<NaviagtableMeiliSearchModel> GetCategoryNaviagtableModel(dynamic category)
        {
            try
            {
                List<Category> categories;
                if(category is List<Category>)
                {
                    categories = category;
                }
                else
                {
                    categories=new List<Category> { category};
                }

                var categoriesvm = categories
                        .Select(x => new NaviagtableMeiliSearchModel()
                        {
                            Id = x.Id,
                            MeiliId = $"{x.Id}-{nameof(EEntity.Category)}",
                            EntityType = $"{nameof(EEntity.Category)}",
                            SiteId = x.SiteId ?? 0,
                            Details = x.CategoryDetails.Select(bd => new Details()
                            {

                                Name = bd.Name,
                                Description = bd.Description,
                                Slug = bd.SlugInfo?.Slug ??string.Empty,
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

                        }).ToList();
               
                return categoriesvm;
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public static List<NaviagtableMeiliSearchModel> GetPageNaviagtableModel(dynamic _pages)
        {
            try
            {
                List<Page> pages;
                if (_pages is List<Page>)
                {
                    pages = _pages;
                }
                else pages = new List<Page> { _pages };
                var allPages = BuildPagesTree(pages);
                var navigatablePages = allPages.Select(p => new NaviagtableMeiliSearchModel()
                {
                    Id = p.ParentPageId ?? 0,
                    MeiliId = $"{p.ParentPageId ?? 0}-{nameof(EEntity.Page)}",
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

        public static List<NaviagtableMeiliSearchModel> GetPostNaviagtableModel(dynamic postrep)
        {
            try
            {
                List<Posts> posts;
                if (postrep is List<Posts>)
                {
                    posts = postrep;
                }
                else posts = new List<Posts> { postrep };
                var allpost =  BuildPostsTree(posts);

                var navigatablePost = allpost.Select(p => new NaviagtableMeiliSearchModel()
                {
                    Id = p.Id,
                    MeiliId = $"{p.Id}-{nameof(EEntity.Post)}",
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

        //Get pages with their cultures
        public static List<PageResultVM> BuildPagesTree(List<Page> pageList)
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
     

      
        public static List<PostsOutputVM> BuildPostsTree(List<Posts> AllPosts)
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
      

    }

}
