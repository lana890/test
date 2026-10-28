
using EEskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.product.request;
using EskaCommerce.Catalog.ViewModels.Category;
using EskaCommerce.Catalog.ViewModels.Category.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces
{
    public interface IProductServices
    {
        Task<long> AddProduct(ProductRequestViewModel model);
        Task<PaginationResult<ProductViewModel>> GetProductByCategoryId(ProductSearchViewModel productSearch);
        Task<long> UpdateProduct(ProductRequestViewModel model);
        Task<List<CatelogFlatViewModel>> GetAllProductFlat(long cultureId, long categoryId);
        Task<List<CatelogFlatViewModel>> GetProductsForAdmin();
        Task<PaginationResult<AllProductsResponseViewModel>> GetProducts(SearchParamsViewModel product);
        Task<(bool, List<long> InValidProducts)> CheckAllowedQuantity(List<CheckProductQuantityVM> products, long customerId);
        Task<List<CatelogSettingsViewModel>> GetAllProductsByCategoryIdsForAdmin(long[] ids);
        Task<ProductResponseViewModel> GetProductById(long id);


        Task<long> GetOriginalProductId(long ProdId);
        Task<Product> GetOriginalProduct(long ProdId);
        Task<bool> ChangeStatus(long id, EStatus status);
        Task<bool> UpsertProductCategoryOrders(UpsertProductCategoryOrderDto model);

        Task<bool> DeleteProductCategory(DeleteProductCategoryDto model);


    }
}
