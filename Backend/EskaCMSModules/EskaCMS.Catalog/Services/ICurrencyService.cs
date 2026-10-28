using System.Threading.Tasks;
using System.Collections.Generic;

using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;

namespace EskaCommerce.Catalog.Services
{
    public interface ICurrencyRatesService
    {
        Task<IList<CurrencyRatesListItem>> GetAll();

        //Task Create(CurrencyRates currencyrates);

        //Task Update(CurrencyRates currencyrates);

        //Task Delete(CurrencyRates currencyrates);
    }
}
