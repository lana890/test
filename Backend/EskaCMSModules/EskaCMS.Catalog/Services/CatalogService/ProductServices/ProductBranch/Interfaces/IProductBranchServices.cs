using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.ProductBranch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductBranch.Interfaces
{
    public  interface  IProductBranchServices
    {
        Task<bool> UpsertProductBranch(RequestProductBranchViewModel model);
        Task<List<ResultProductBranchViewModel>> GetProductBranchById(long ProductId);
    }
}
