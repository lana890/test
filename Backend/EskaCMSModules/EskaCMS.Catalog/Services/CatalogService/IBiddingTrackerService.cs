using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Services.CatalogService
{
    public interface IBiddingTrackerService
    {
        IList<BiddingTrackerVM> GetAll();
        Task<IList<BiddingTrackerVM>> GetAllAsync();
        Task<BiddingTrackerVM> GetByIdAsync(long Id);
    }
}
