using EskaCMS.Integrations.Models;
using EskaCMS.Integrations.Models.Insurance;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace EskaCMS.Integrations.Services.Interfaces
{
    public interface IGeneralApiClient
    {
         Task DeleteAttachment(string documentId);

		Task<object> UpdateQuotation(long customerId, JObject cart, string documentId, JObject generalRequest);
        Task InsertAttachments(List<IdFiles> files, string documentId);
        Task InsertQuotationInterest(string documentId, long customerId, JObject cart, JObject generalRequest);
        Task InsertQuotationCovers(string documentId);
        Task CalculateQuotation(string documentId);
        Task CalculateQuotationFinal(string documentId);
        Task CheckApproval(string documentId);
        Task UpdateQuotationStatus(string documentId);
        Task<ConvertToPolicyResponse> ConvertToPolicy(string documentId);

        IntegrationSettings Settings { get; }


    }
}