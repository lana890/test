 using EskaCMS.Integrations.Helpers;
using EskaCMS.Integrations.Models.Insurance;
using EskaCMS.Integrations.Services.Interfaces;
using Newtonsoft.Json.Linq;
using System;
 using System.Net.Http.Headers;
 

namespace EskaCMS.Integrations.Services.InsuranceInterfaces
{
  
    public class MedicalApiClient : HttpServiceBase, IMedicalApiClient
    {
        private readonly IntegrationSettings _settings;

        public MedicalApiClient(HttpClient client) : base(client)
        {
            _settings = IntegrationSettingsLoader.Load<IntegrationSettings>(AppContext.BaseDirectory, "Integration");

            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.LongLifeToken);
            Client.DefaultRequestHeaders.Remove("Environment");
            Client.DefaultRequestHeaders.Add("Environment", "onpremise");
            Client.DefaultRequestHeaders.Remove("SessionId");
            Client.DefaultRequestHeaders.Add("SessionId", "");
        }

        public async Task<object> UpdatePolicy(
            long customerId, JObject cart, string documentId, JObject generalRequest)
        {
            var now = DateTime.Now;

            var payload = new UpdatePolicy
            {
                CreatedBy        = "ONLINE",
                ModifiedBy       = "ONLINE",
                APPLICANT_TYPE   = "2",
                APPROVAL_STATUS  = "1",
                MPD_APP_ID       = null,
                PolicyID         = documentId,
                PolicyHolderID   = customerId.ToString(),
                Exrate           = generalRequest.GetStringSafe("Exrate"),
                PlanID           = generalRequest.GetStringSafe("PlanID"),
                CurrencyCode     = generalRequest.GetStringSafe("CurrencyCode"),
                CompanyID        = generalRequest.GetStringSafe("CompanyID"),
                BranchID         = generalRequest.GetStringSafe("BranchID"),
                TpaType          = generalRequest.GetStringSafe("TpaType"),
                ReceviedDate     = generalRequest.GetDateSafe("ReceviedDate") ?? now,
                AccountedFor     = generalRequest.GetStringSafe("AccountedFor"),
                DURATION         = generalRequest.GetStringSafe("DURATION"),
                DURATION_UNIT    = generalRequest.GetStringSafe("DURATION_UNIT"),
                ReferenceNo      = generalRequest.GetStringSafe("ReferenceNo"),
                APPLICANT_NAME   = generalRequest.GetStringSafe("APPLICANT_NAME"),
                ExpiryDate       = generalRequest.GetDateSafe("ExpiryDate") ?? now,
                PaymentID        = generalRequest.GetStringSafe("PaymentID"),
                PolicyType       = generalRequest.GetStringSafe("PolicyType"),
                UWYear           = generalRequest.GetStringSafe("UWYear"),
                ReferenceDate    = generalRequest.GetDateSafe("ReferenceDate") ?? now,
                IssueDate        = generalRequest.GetDateSafe("IssueDate")    ?? now,
                EffectiveDate    = generalRequest.GetDateSafe("EffectiveDate") ?? now,
                BusinessType     = generalRequest.GetStringSafe("BusinessType"),
                DocumentType     = generalRequest.GetStringSafe("DocumentType"),
                CalculationType  = generalRequest.GetStringSafe("CalculationType"),
                ProductID        = generalRequest.GetStringSafe("ProductID"),
                QuotationType    = generalRequest.GetStringSafe("QuotationType"),
                CRM_LED_ID       = generalRequest.GetStringSafe("CRM_LED_ID"),
                EndorsementID    = generalRequest.GetStringSafe("EndorsementID"),
                FamilyClassFlag  = generalRequest.GetStringSafe("FamilyClassFlag"),
                DiscountPercent  = generalRequest.GetStringSafe("Discount%"),
                CreationDate     = now,
                ModificationDate = now
            };

            return await PostAsync<object>(
                $"{_settings.MedicalBaseUrl}/api/Policy/UpdatePolicy", payload);
        }

        public async Task<ConvertQuotationARTResponseVm> ConvertQuotationART(string documentId) =>
            await PostAsync<ConvertQuotationARTResponseVm>(
                $"{_settings.MedicalBaseUrl}/api/Quotation/ConvertQuotationART",
                new ConvertQuotationART { Created_by = "ONLINE", ConvertMembers = "true", ID = documentId });

        public async Task<object> PostPolicy(string documentId) =>
            await PostAsync<object>(
                $"{_settings.MedicalBaseUrl}/api/Policy/PostPolicy",
                new PostPolicy { ID = int.Parse(documentId), lcFeesId = "7", CREATED_BY = "ONLINE" });

        public async Task InitializeNotification(string documentId) =>
            await PostAsync<object>(
                $"{_settings.MedicalBaseUrl}/api/Quotation/InitializeNotificatoin",
                new InitializeNotificatoin { PolicyID = int.Parse(documentId) });
    }
}