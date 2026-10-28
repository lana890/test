using EskaCMS.Core.Models;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.BrandVm;
using EskaCMS.Entities.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCommerce.Catalog.ViewModels.Category.Responses;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Pagination;
using System;

namespace EskaCommerce.Catalog.Services.CatalogService.BrandServices.Interfaces
{
    public interface IBrandService
    {

        Task<PaginationResult<BrandViewModel>> GetAllBrand(BrandParamsViewModel model, long SiteId);

        Task<bool> AddBrand(BrandViewModel model, long UserId = 0, long SiteId = 0);
         Task<bool> UpdateBrand(BrandViewModel model, long UserId = 0, long SiteId = 0);

        Task<bool> ChangeBrandStatus(long id, EStatus status, long UserId = 0);

        Task<BrandViewModel> GetBrandById(long id);



         Task<List<BrandForProductViewModel>> GetBrands( long siteId);




       Task<List<CatelogFlatViewModel>> GetAllBrandFlat(long CultureId, long Siteid);
    }
}
