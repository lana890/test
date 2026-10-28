using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces;
using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Migrations.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea
{
    public class UserWishListServices:IUserWishList
    {
        private readonly IRepository<UserWishList> _UserWishList;
        private readonly IProductWebServices _productWebServices;
        private readonly IRepository<Product> _productsRepository; 
        private readonly IWorkContext _workContext;
        public UserWishListServices(IRepository<UserWishList> UserWishList, IProductWebServices productWebServices,IRepository<Product> productRepository , IWorkContext workContext)
        {
            _UserWishList = UserWishList;
            _productWebServices = productWebServices;
            _productsRepository = productRepository;
            _workContext = workContext;
        }



        public async Task<ProductViewModelForWeb> GetWishList( WishListProductVM wishListProductVM)
        {
            try
            {

                var siteId = _workContext.GetCurrentSiteId();
                var userId = await _workContext.GetCurrentUserId();
                ProductParamswithIdVM productParamsVM = new ProductParamswithIdVM();

                var ProductIds = await GetWishlistProductIdsAsync(userId,siteId);

                if (!ProductIds.Any())
                {
                    return new ProductViewModelForWeb()
                    {
                        Catname="",
                        products = new PaginationResult<ProductForWeb>(new List<ProductForWeb>(),0,0),
                    };
                }


                    productParamsVM.ProductIds =ProductIds;
                    productParamsVM.PageNumber = wishListProductVM.PageNumber;
                    productParamsVM.PageSize = wishListProductVM.PageSize;
                    productParamsVM.Sort = new ProductSearchSort {
                    SortField= [],
                    SortType= ESortType.desc
                    
                    };
                


                var result = await _productWebServices.GetProductsbyIdsForWeb(productParamsVM);

                result.products.Data = result.products.Data
                    .OrderBy(x => ProductIds.IndexOf(x.Id))
                    .ToList();



                return result;


            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<bool> RemoveProductFromWishList(long Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var userId = await _workContext.GetCurrentUserId();
                var Product = await _UserWishList.Query()
                                     .Where(x => x.ProductId == Id && x.SiteId == siteId && x.UserId == userId && x.Status == EStatus.Published).FirstOrDefaultAsync();
                if (Product == null)
                {
                    throw new Exception("Product Not Found");
                }
                Product.Status = EStatus.Deleted;
                Product.ModificationDate = DateTimeOffset.Now;
                _UserWishList.Update(Product);
                await _UserWishList.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<bool> AddWishListItem(long productId)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();

                var userId = await _workContext.GetCurrentUserId();

                var wishListProduct = await _UserWishList.Query().Where(x =>x.UserId == userId && x.ProductId == productId && x.SiteId == siteId).FirstOrDefaultAsync();

                if (wishListProduct != null && wishListProduct.Status== EStatus.Deleted)
                {
                    wishListProduct.Status = EStatus.Published; 
                    _UserWishList.Update(wishListProduct);
                    await _UserWishList.SaveChangesAsync();

                }
                else
                {
                    var userWishList = new UserWishList(); 
                    userWishList.ProductId = productId;
                    userWishList.Status = EStatus.Published;
                    userWishList.CreationDate = DateTimeOffset.Now;
                    userWishList.UserId = userId;
                    userWishList.SiteId= siteId;
                    _UserWishList.Add(userWishList);
                    await _UserWishList.SaveChangesAsync();

                }

                return true;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<ProductViewModelForWeb> GetSimilarItemsForWishlist(WishListProductVM wishListProductVM)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var userId = await _workContext.GetCurrentUserId();
                var WarehouseId = await _workContext.GetCurrentWarehouseId();

                var wishlistProductIds = await GetWishlistProductIdsAsync(userId, siteId);

                if (!wishlistProductIds.Any())
                {
                    return new ProductViewModelForWeb
                    {
                        Catname = "",
                        products = new PaginationResult<ProductForWeb>(new List<ProductForWeb>(), 0, 0),
                    };
                }

                var wishlistProfile = await _productsRepository.Query()
                    .AsNoTracking()
                    .Where(x => wishlistProductIds.Contains(x.Id))
                    .Select(x => new
                    {
                        CategoryIds = x.Categories.Select(c => c.CategoryId),
                        AttributeValueIds = x.AttributeValues.Select(av => av.Id)
                    })
                    .ToListAsync();

                var wishlistCategoryIds = wishlistProfile.SelectMany(x => x.CategoryIds).Distinct().ToList();
                var wishlistAttributeValueIds = wishlistProfile.SelectMany(x => x.AttributeValueIds).Distinct().ToList();

                if (!wishlistCategoryIds.Any())
                {
                    return new ProductViewModelForWeb
                    {
                        Catname = "",
                        products = new PaginationResult<ProductForWeb>(new List<ProductForWeb>(), 0, 0),
                    };
                }

                // Score candidates: same category as anything in the wishlist,
                var scored = await _productsRepository.Query()
                    .AsNoTracking()
                    .Where(x =>
                        x.SiteId == siteId &&
                        x.Status == EStatus.Published &&
                        !wishlistProductIds.Contains(x.Id) &&
                        x.Categories.Any(c => wishlistCategoryIds.Contains(c.CategoryId)))
                    .Select(x => new
                    {
                        ProductId = x.Id,
                        AttributeMatchCount = x.AttributeValues.Count(av => wishlistAttributeValueIds.Contains(av.Id))
                    })
                    .ToListAsync();

                var rankedIds = scored
                    .OrderByDescending(x => x.AttributeMatchCount)
                    .Take(wishListProductVM.PageSize)
                    .Select(x => x.ProductId)
                    .ToList();

                if (!rankedIds.Any())
                {
                    return new ProductViewModelForWeb
                    {
                        Catname = "",
                        products = new PaginationResult<ProductForWeb>(new List<ProductForWeb>(), 0, 0),
                    };
                }

                ProductParamswithIdVM productParamsVM = new ProductParamswithIdVM();
                productParamsVM.ProductIds = rankedIds;
                productParamsVM.PageNumber = wishListProductVM.PageNumber;
                productParamsVM.PageSize = wishListProductVM.PageSize;
                productParamsVM.Sort = new ProductSearchSort
                {
                    SortField = Array.Empty<int>(),
                    SortType = ESortType.desc
                };

                var result = await _productWebServices.GetProductsbyIdsForWeb(productParamsVM);

                result.products.Data = result.products.Data
                    .OrderBy(x => rankedIds.IndexOf(x.Id))
                    .ToList();

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #region Helpers
        private async Task<List<long>> GetWishlistProductIdsAsync(long userId, long siteId)
        {
            return await _UserWishList.Query()
                .Where(x => x.UserId == userId && x.Status == EStatus.Published && x.SiteId == siteId)
                .OrderByDescending(x => x.Id)
                .Select(p => p.ProductId)
                .ToListAsync()??[];
        }
        #endregion
    }
}
