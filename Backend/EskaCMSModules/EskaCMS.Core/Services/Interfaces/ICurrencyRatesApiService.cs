using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Web.SmartTable;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
    public interface ICurrencyRatesApiService
    {
        IEnumerable GetAllCurrencies(long SiteId);

        IEnumerable GetAllCurrencies(string Culture);
        IEnumerable GetAllCurrencies();
        Task<IList<CultureVM>> GeUsedCultures();
        SmartGridOutputVM<CurrencyRatesVM> GetCurrencies(SmartSearchTableParam<CurrencyRatesSearchListVM> param,long SiteId);
        Task <CurrencyRatesVM> GetCurrencyById(long Id);
        Task<bool> Create(CurrencyRatesVM currency, long siteId);
        Task<bool> Edit(CurrencyRatesVM currency);
        Task<bool> Delete(long Id);
    }
}
