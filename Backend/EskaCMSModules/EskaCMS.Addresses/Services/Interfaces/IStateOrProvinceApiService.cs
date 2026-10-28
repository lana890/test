using EskaCMS.Addresses.Model;
using EskaCMS.Addresses.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Web.SmartTable;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EskaCMS.Addresses.Services.Interfaces
{
    public interface IStateOrProvinceApiService
    {
        StateSmartGridOutputVM<StateOrProvinceForm> GetStatesByCountryId(StateSmartSearchTableParam<StateSearchListVM> param, string countryId, long SiteId);
        Task<List<StateOrProvinceForm>> GetStatesByCountryId(string countryId, long siteId);
        Task<StateOrProvinceResponseRequestForm> GetStateById(long Id);
        Task<bool> EditState(long Id, StateOrProvinceResponseRequestForm model);
        Task<bool> CreateState(StateOrProvinceResponseRequestForm model, long SiteId);
        Task<bool> DeleteState(long Id);
        Task<List<ShippableStatesVM>> GetShippableStates(string countryId, int? PageSize = 20,int? PageNumber=0, string Search = null,long? StateId=null);
    }
}
