using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Inventory.Areas.Inventory.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Inventory.Services.Interface
{
    public interface IWarehouseServices
    {
        Task<bool> UpdateWarehouseRoles(long Id, List<long> rolesIdsList);
        Task<PaginationResult<WarehouseListVM>> GetAllWarehouses(SmartTableParam model);

        Task<Address> GetAddressByWarehouseId(long WarehouseId);
        Task<long> GetMainWareHouse(long SiteId);
        Task<WarehouseAssignedRolesVM> GetWareouseRoles(long Id);



        Task<PaginationResult<WarehouseWorkingHoursViewModel>> GetWarehouseWorkingHours(long warehouseId, SmartTableParam param);
        Task<List<WarehouseWorkingHoursViewModel>> GetWarehouseWorkingHourById(long id);
        Task<bool> CreateWarehouseWorkingHour(List<CreateWarehouseWorkingHoursRequest> model);
        Task<bool> DeleteWarehouseWorkingHour(long id);
    }
}
