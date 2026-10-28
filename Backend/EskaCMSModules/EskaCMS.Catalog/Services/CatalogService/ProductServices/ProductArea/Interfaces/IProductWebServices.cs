using EEskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.Tags;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces
{
    public interface IProductWebServices
    {
        Task<ProductVariationDetailsVM> GetProductVariationDetails(long? Id = null, string slug = null);
        Task<ProductReviewDetailsVM> GetProductRevieweDetails(long? Id = null, string slug = null);
        Task<LinkedProductDetailsVM> GetProductLinksDetails(long? Id = null, string slug = null);
        Task<ProductAttributeDetailsVM> GetProductAttributeDetails(long? Id = null, string slug = null);
        Task<ProductCategoryDetailsVM> GetProductCategoryDetails(long? Id = null, string slug = null);
        Task<ProductOptionDetailsVM> GetProductOptionsDetails(long? Id = null, string slug = null);
        Task<ViewProductDetailaVM> ViewProductDetails(long? Id = null, string slug = null);


        Task<ProductDetail> ProductOverview(long id);

        Task<ProductDetail> ProductDetails(long? Id = null, string Slug = null);
        Task<ProductDetail> GetMappingProductByAttributeValue(MappingProductVM model);

        Task<PaginationResult<ProductWebViewModel>> GetProductByCategoryId(ProductSearchViewModel productSearch);
        //Task<ProductViewModelForWeb> GetProductsbyCategoryIdForWeb(ProductParamsViewModel model);

        Task<List<CatelogSettingsViewModel>> GetProductsByProductsIds(long[] ids);
        Task<ProductViewModelForWeb> GetProductsbyIdsForWeb(ProductParamswithIdVM productParamsVM);
        Task<ProductViewModelForWeb> GetProductsbyCategoryIdForWeb(ProductParamswithCategoryIdVM model);
        Task<ProductViewModelForWeb> GetMostSellingProducts(ProductParamsVM model);
        Task<ProductViewModelForWeb> GetProductsbyCategoryIds(ProductParamsViewModel model);
        Task<MinMaxProductsViewModel> GetMinAndMaxPriceProduct();
        Task<List<GetTagsViewModel>> GetProductTagsByCategoryId(ProductTagsViewModel product);
        Task<PaginationResult<ProductForWeb>> GetAllProducts(ProductFiltrationViewModel model);
        Task<PaginationResult<GetProductAttributeGroupViewModel>> GetProductAttributeByCategoryId(FilterProductAttributeViewModel model);
        Task<ProductViewModelForWeb> GetTodaysOffers(ProductOffersVM productOffersVM);
    }
}
