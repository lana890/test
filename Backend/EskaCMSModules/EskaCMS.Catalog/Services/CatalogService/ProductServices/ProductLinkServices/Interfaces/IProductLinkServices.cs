using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Module.Core.Areas.Core.ViewModels;
using EskaCMS.Pages.Models;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductLinkServices.Interfaces
{
    public interface IProductLinkServices
    {
        Task<List<long>> GetProductsCategory(long ParentId);
        Task<bool> UpsertProductsCategory(ProductCategoryViewModel model);
        Task<bool> DeleteProductLinks(long ProductId);
        Task<bool> DeleteLink(long LinkId);
        Task<bool> UpsertProductLinks(List<ItemLinkVm> ProductLinks, long ProductId, ItemLinkType itemLinkType);
        Task<List<ProductLinkViewModel>> GetLinkedProducts(long ProductId, ItemLinkType linkType);
        Task<PaginationResult<ProductThumbnail>> GetLinkedProductsForWeb(SmartTableParam param, long productId);
    }
}
