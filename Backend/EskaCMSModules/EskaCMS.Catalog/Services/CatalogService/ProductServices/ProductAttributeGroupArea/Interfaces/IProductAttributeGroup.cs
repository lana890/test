using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.ProductAttribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductAttributeGroupArea.Interfaces
{
    public interface IProductAttributeGroup
    {
        Task<bool> AddProductAttributeGroup(AtrtributeGroupViewModel model);

       Task<bool> UpdateProductAttributeGroup(AtrtributeGroupViewModel model);


       Task<PaginationResult<AtrtributeGroupViewModel>> GetProductAttributeGroups(AttributeParams model);

         Task<List<GetGroupsFlat>> GetProductAttributeGroupsFlat();

        Task<AtrtributeGroupViewModel> GetProductAttributeGroupById(long Id);


         Task ChangeAttributeGroupStatus(long Id, EStatus NewStatus);

        Task<List<AttributeWithGroupViewModel>> GetAttributeWithGroup(long CultureId);

    }
}
