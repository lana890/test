using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Tax;

namespace EskaCMS.Tax.Services
{
    public class TaxService : ITaxService
    {
        private readonly IRepository<TaxRate> _taxRateRepository;

        public TaxService(IRepository<TaxRate> taxRateRepository)
        {
            _taxRateRepository = taxRateRepository;
        }

        public async Task<decimal> GetTaxPercent(long? taxClassId, string countryId, long? stateOrProvinceId, string zipCode)
        {
            if (!taxClassId.HasValue)
            {
                return 0;
            }

            var query = _taxRateRepository.Query()
                           .Where(x => x.CountryId == countryId
                           && (x.StateOrProvinceId == stateOrProvinceId || x.StateOrProvinceId == null)
                           && x.TaxClassId == taxClassId.Value);
            if (!string.IsNullOrEmpty(zipCode) && stateOrProvinceId != null)
            {
                query = query.Where(x => x.ZipCode == zipCode);
            }


            var taxRate = await query.FirstOrDefaultAsync();
            if (taxRate != null)
            {
                return taxRate.Rate;
            }

            return 0;
        }

        public async Task<decimal> GetTaxPercent(long? taxClassId, string countryId, long? stateOrProvinceId)
        {
            if (!taxClassId.HasValue)
            {
                return 0;
            }

            var query = _taxRateRepository.Query()
                           .Where(x => x.CountryId == countryId
                           && (x.StateOrProvinceId == stateOrProvinceId || x.StateOrProvinceId == null)
                           && x.TaxClassId == taxClassId.Value);
           



            var taxRate = stateOrProvinceId.HasValue ? query.FirstOrDefault(x=>  x.StateOrProvinceId == stateOrProvinceId.Value) : null;
            if (taxRate == null)
            {
                taxRate = query.FirstOrDefault(x => x.StateOrProvinceId == null);
            }
            if (taxRate != null)
            {
                return taxRate.Rate;
            }

            return 0;
        }
    }
}
