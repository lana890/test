using EskaCMS.Addresses.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Web.SmartTable;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EskaCMS.Addresses.Services.Interfaces
{
    public interface IDistrictApiService
    {
        DistrictSmartGridOutputVM<DistrictVM> GetDistrictsByStateId(DistrictSmartSearchTableParam<DistrictSearchListVM> param, long stateId, long SiteId);
        Task<DistrictRequestResponseVM> GetDistrictById(long Id);
        Task<bool> EditDistrict(long Id, DistrictRequestResponseVM model);
        Task<bool> CreateDistrict(DistrictRequestResponseVM model, long SiteId);
        Task<bool> DeleteDistrict(long Id);
    }
}
