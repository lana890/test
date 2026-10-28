using EskaCMS.Addresses.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Web.SmartTable;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EskaCMS.Addresses.Services.Interfaces
{
    public interface ICountryApiService
    {
        CountrySmartGridOutputVM<CountryForm> GetCountries(CountrySmartSearchTableParam<CountrySearchListVM> param, long SiteId);
        Task<CountryRequestForm> GetCountryById(string Id);
        Task<bool> EditCountry(string Id, CountryRequestForm model);
        Task<bool> CreateCountry(CountryRequestForm model, long SiteId);
        Task<bool> DeleteCountry(string Id);
        Task<List<CountryReponseDto>> ShippingEnabled(bool IsShippingEnabled);
        Task<List<CountryReponseDto>> GetUsedCountries();
        Task<List<CountryForm>> GetEnabledCountry(long siteId);
        Task<IEnumerable> GetAllCountries();

    }
}
