using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Requests;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses;
using EskaCommerce.Catalog.ViewModels.Category.Responses;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Services.CatalogService.CategoryServices.Interfaces
{
    public interface ICategoryWebServices
    {
        
        Task<CategoryWebViewModel> GetById(long Id);
        Task<List<CategoryWebResponseViewModel>> GetCategoriesNavigator(CategoryWebVM Model);
        Task<List<CategoryWebViewModel>> GetCategoriesByIds(List<long> ids, long? SiteId);

        Task<CategoryWebViewModel> GetCategoriesById(long CategoryId);
        Task<List<CategoriesBreadCrumbVM>> GetCategoryBreadCrumb(long categoryId);

        Task<List<long>> GetCategoriesByBrandId(long brandId);
        Task<List<CatelogFlatViewModel>> GetAllCategoriesByTypeId(long categoryTypeId,  bool? isFeatured = null);
        Task<PaginationResult<CategoryWebViewModel>> GetAllCategories(CategoryFiltrationViewModel model);
    }
}
