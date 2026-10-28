

using EskaCommerce.Inventory.Services;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Inventory;

using EskaCMS.Entities.Entity;
using EskaCommerce.Inventory.Areas.Inventory.ViewModels;
using System.Collections.Generic;
namespace EskaCommerce.Inventory.Services
{
    public interface IStockService
    {
        Task AddAllProduct(Warehouse warehouse);

        Task UpdateStock(StockUpdateRequest stockUpdateRequest);
        Task<List<GetProductsStockViewModel>> GetAvailableStockByProductId(long productId);
        Task<bool> UpsertProductStock(List<AddEditProductStockViewModel> model);
        Task<bool> ChangeStockStatus(StockStatusViewModel model);
    }
}
