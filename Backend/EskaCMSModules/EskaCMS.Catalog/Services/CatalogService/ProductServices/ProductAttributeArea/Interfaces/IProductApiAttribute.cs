using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;

using EskaCommerce.Catalog.Areas.Catalog.ViewModels.ProductAttribute;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductAttributeArea.Interfaces
{
    public interface IProductApiAttribute
    {
        Task<PaginationResult<ProductAttributeViewModel>> List(AttributeParams attributeParams);

        Task<ProductAttributeViewModel> GetAttributeById(long Id);
        Task<List<GetProductValueVM>> GetAttributeValuebyProductId(long ProductId);

        Task<bool> AddProductAttributeValue(List<RequestProductValueVM> productAttributeVM, long ProductId);
        Task<List<ProductAttributeGlobalVM>> GetProductAttributeValue(long ProductId);

        Task<bool> AddProductAttribute(ProductAttribuateRequestViewModel model);

        Task<bool> UpdateProductAttribute(ProductAttribuateRequestViewModel model);




        Task<bool> ChangeProductAttributeStatus(long Id, EStatus status);

        Task<List<ProductAttribuateResponseViewModel>> GetAttributeByTemplateId(long TemplateId);
        Task<bool> UpdateProductAttributeValue(List<GetProductValueVM> productAttributeVM);
    }
}
