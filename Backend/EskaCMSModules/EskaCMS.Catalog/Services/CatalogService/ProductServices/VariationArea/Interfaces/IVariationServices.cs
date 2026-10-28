using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.VariationArea.Interfaces
{
    public interface IVariationServices
    {
        Task<bool> AddProductVariations(List<VariationsViewModel> Variations, long ProductId);
        Task<bool> EditProductVariations(List<VariationsViewModel> Variations, long ProductId);

        Task<bool> DeleteAllVariations(long ProductId);

        Task<List<VariationsViewModel>> GetProductVaration(long ProductId);
        Task<bool> DeleteProductVariation(long ProductLinkId);

        Task<ProductWithVariationsVM> GetProductAndVariation(long VariationId);
        Task<List<VariationsViewModel>> GetProductVarationForAdmin(long ProductId);

        Task<List<VariationsViewModel>> ModifyCombinations(List<VariationsViewModel> Variations, long ProductId);
        Task<bool> ChangeOrderingForOptions(long ProductId, List<ChangeAvailableOptionOrderVM> model);
    }
}
