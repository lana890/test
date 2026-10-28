using EskaCMS.Integrations.Models.Insurance;
using EskaCMS.Integrations.Models.Insurance;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Integrations.Services.Interfaces
{
    public interface IMedicalApiClient
    {
        Task<object> UpdatePolicy(long customerId, JObject cart, string documentId, JObject generalRequest);
        Task<ConvertQuotationARTResponseVm> ConvertQuotationART(string documentId);
        Task<object> PostPolicy(string documentId);
        Task InitializeNotification(string documentId);
    }
}
