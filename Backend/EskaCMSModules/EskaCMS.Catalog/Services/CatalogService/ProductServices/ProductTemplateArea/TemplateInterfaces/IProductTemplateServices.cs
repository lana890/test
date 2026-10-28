using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.ProductAttribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductTemplateArea.TemplateInterfaces
{
    public interface IProductTemplateServices
    {
        Task<PaginationResult<ProductTemplateResponseViewModel>> GetProductTemplate(long SiteId, AttributeParams attributeParams);
        Task<List<TemplateModel>> GetProductTemplateList();
        Task<List<TemplateAttributeViewModel>> GetProductTemplateById(long Id);
        Task<ProductTemplatebyCultureViewModel> GetTemplateByCultureId(long CultureId);
        Task<bool> AddProductTemplate(ProductTemplateViewModel model, long SiteId);
        Task<bool> UpdateProductTemplate(ProductTemplateViewModel model);
        Task<bool> ChangeProductTemplateStatus(long Id, EStatus NewStatus);

    }
}
