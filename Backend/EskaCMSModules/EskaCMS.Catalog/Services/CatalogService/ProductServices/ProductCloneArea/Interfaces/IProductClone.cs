using EskaCMS.Core.Areas.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductCloneArea.Interfaces
{
    public interface IProductClone
    {
        Task<ItemCloneFormVm> GetProductClone(long id);
        Task<bool>AddProductClone(ItemCloneFormVm colne);
    }
}
