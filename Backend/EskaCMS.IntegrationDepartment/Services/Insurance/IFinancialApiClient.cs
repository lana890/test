using EskaCMS.Integrations.Models.Insurance;
using System.Threading.Tasks;

namespace EskaCMS.Integrations.Services.Interfaces
{
    public interface IFinancialApiClient
    {
        Task GenerateReceipt(string policyId, long? transactionId, string netPremium, string date);
        Task<long> GetCustomerByNationalId(string nationalId);
        Task<long> InsertCustomer(InsertCustomerRequest payload);
        Task UpdateCustomer(UpdateCustomerRequest payload);
    }
} 