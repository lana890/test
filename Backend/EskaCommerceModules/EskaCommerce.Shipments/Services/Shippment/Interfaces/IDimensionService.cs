using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Shipments.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Shipments.Services.Shippment.Interfaces
{
    public interface IDimensionService
    {
        Task<PaginationResult<DimensionViewModel>> GetAllAsync(DimensionFilterationViewModel model);
        Task<DimensionViewModel> GetByIdAsync(int id);
        Task<bool> CreateAsync(DimensionCreateUpdateViewModel model);
        Task<bool> UpdateAsync( DimensionCreateUpdateViewModel model);
        Task<bool> DeleteAsync(int id);

    }

}
