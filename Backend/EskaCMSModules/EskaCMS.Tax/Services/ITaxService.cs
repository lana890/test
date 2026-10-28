using System.Threading.Tasks;

namespace EskaCMS.Tax.Services
{
    public interface ITaxService
    {
        Task<decimal> GetTaxPercent(long? taxClassId, string countryId, long? stateOrProvinceId, string ZipCode);
        Task<decimal> GetTaxPercent(long? taxClassId, string countryId, long? stateOrProvinceId);
    }
}
