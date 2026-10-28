using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductFeesArea.Interfaces
{
    public interface IProductFeesService
    {
        Task<bool> UpsertProductFees(ProductFeesModel prodfees, long SiteId);


        Task<long> EditProductFees(long FeesId,ProductFeesVM prodfees, long SiteId);


        Task<List<ProductFeesVM>> GetAdditionalFeesByProductId(long ProductId);

        Task<ProductFeesVM> GetAdditionalFeesById(long Id);
        Task<bool> DeleteProductFees(long Id);

    }
}
