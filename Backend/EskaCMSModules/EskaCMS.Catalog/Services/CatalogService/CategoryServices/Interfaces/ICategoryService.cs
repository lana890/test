using System.Threading.Tasks;
using System.Collections.Generic;

using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Catalog.ViewModels.Category;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Requests;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses;
using EskaCommerce.Catalog.ViewModels.Category.Responses;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.product.request;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.product.Response;

namespace EskaCommerce.Catalog.Services.CatalogService.CategoryServices.Interfaces
{
    public interface ICategoryService
    {
        #region Admin
        Task<long> AddCategory(CategoryRequestViewModel model);
        Task<bool> UpdateCategory(CategoryRequestViewModel model);
        Task<bool> ChangeStatus(long CatId, EStatus Status);
        Task<List<CategoryListVm>> GetCategories();
        List<object> GetCategoriesType();
        Task< PaginationResult<AllCategoryViewModel>> GetCategoriesbySiteId(CategorySearchSiteViewModel model);
        Task<List<CatelogFlatViewModel>> GetAllCategoriesFlat(long CultureId);
        Task<PaginationResult<CategoryAdminViewModel>> GetAllCategories(CategoryParamsViewModel categoryParamsVM);
        Task<bool> ChangeCategoryStatusByParentId(long ParentId, EStatus status);
        Task<CategoryAdminViewModel> GetCategoriesById(long CategoryId);
        Task<List<CategoryAdminViewModel>> GetCategoriesByTypeId(ECategoryTypes CategoryTypeId, long? SiteId, long? ParentId, long? CultureId, bool WithShared = false);
        Task<List<CategoryAdminViewModel>> GetCategoriesTreeByTypeId(ECategoryTypes CategoryTypeId, long? SiteId, long? ParentId, long? CultureId, bool WithShared = false);
        Task<List<CategoryAdminViewModel>> GetCategoryByParentId(long parentId);
        Task<SmartTableResult<ProductSmartTableVm>> GetProducts(long id, SmartTableParam param);

        Task<bool> UpdateProductCategory(long id, ProductCategoryForm model);

        #endregion

    }
}

