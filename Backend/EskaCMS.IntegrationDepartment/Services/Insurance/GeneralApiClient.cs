using EskaCMS.Integrations.Services.Interfaces;
using EskaCMS.Integrations.Helpers;
using EskaCMS.Integrations.Models;
using EskaCMS.Integrations.Models.Insurance;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EskaCMS.Integrations.Services;

namespace EskaCMS.Integrations.Services.InsuranceInterfaces
{
   
    public class GeneralApiClient : HttpServiceBase, IGeneralApiClient
    {
        public IntegrationSettings Settings { get; }

        public GeneralApiClient(HttpClient client) : base(client)
        {
            Settings = IntegrationSettingsLoader.Load<IntegrationSettings>(AppContext.BaseDirectory, "Integration");

            // Set once at construction — never mutated per-call (thread-safe).
            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Settings.LongLifeToken);
            Client.DefaultRequestHeaders.Remove("Environment");
            Client.DefaultRequestHeaders.Add("Environment", "onpremise");
            Client.DefaultRequestHeaders.Remove("SessionId");
            Client.DefaultRequestHeaders.Add("SessionId", "");
        }


        // ─── QUOTATION PIPELINE ───────────────────────────────────────────────────

        public async Task<object> UpdateQuotation(
            long customerId, JObject cart, string documentId, JObject generalRequest)
        {
            var now = DateTime.Now;
            var expiryDate = generalRequest.GetDateSafe("ExpiryDate") ?? now;
            var EffectiveDate = generalRequest.GetDateSafe("EffectiveDate") ?? now;
			var VoucherDate = generalRequest.GetDateSafe("VoucherDate") ?? now;

			var QuotationDate = generalRequest.GetDateSafe("QuotationDate") ?? now;
			var QuotationValidity = generalRequest.GetDateSafe("QuotationValidity") ?? now;

			var payload = new UpdateQuotation
            {
                GeneralQuotation = new GeneralQuotation
                {
                    CurrencyCode              = Settings.DefaultCurrency,
                    ExchangeRate              = "1.0",
                    IssueDate                 = now,
                    EffectiveDate             = EffectiveDate,
                    ExpiryDate                = expiryDate,
                    QuotationDate             = now,
                    QuotationExpiryDate       = expiryDate,
                    VoucherDate               = now,
                    AccountedFor              = "1",
                    OpenCoverType             = "0",
                    CreatedBy                 = "ONLINE",
                    Insured                   = customerId.ToString(),
                    NationalID                = cart.GetStringSafe("NationalId", "GaNationalId"),
                    InsuredName               = cart.GetStringSafe("InterestName"),
                    InsuredAddress            = generalRequest.GetStringSafe("Address", "InsuredAddress") ?? "Jordan-Amman",
                    PaymentCycleID            = "1",
                    ClassID                   = generalRequest.GetStringSafe("ClassID"),
                    BusinessType              = "1",
                    PolicyType                = generalRequest.GetStringSafe("PolicyType"),
                    PolicyDays                = generalRequest.GetStringSafe("PolicyDays"),
                    BranchID                  = Settings.BranchId.ToString(),
                    CompanyID                 = Settings.CompanyId.ToString(),
                    FiscalYear                = DateTime.Now.Year.ToString(),
                    CalculationBase           = "4",
                    QuotationValidity         = QuotationValidity.ToString(),
                    QuotationType             = "2",
                    AgentType                 = "1",
                    BranchCurrency            = "JOD",
                    BranchExrate              = "1.0",
                    DocumentID                = documentId,
                    ModifiedBy                = "ONLINE",
                    IsPrinted                 = "0",
                    IsReinsured               = "0",
                    IsConverted               = "0",
                    IsUpdateForConvert        = false,
                    IsCalculated              = "0",
                    PrintCount                = "0",
                    SalesType                 = "0",
                    Beneficiary               = customerId.ToString(),
                    DocumentNo                = "0",
                    NetPremium                = "0",
                    GrossPremium              = "0",
                    TotalFees                 = "0",
                    CommissionAmount          = "0",
                    ExcessAmount              = "0",
                    LiabilityLimit            = "0",
                    TotalInstallmentsAmount   = "0",
                    OurSharePercentage        = "0",
                    AgtCommissionAmount       = "0",
                    IntLoadingSurvayId        = "0",
                    ProspectLeadLocation      = "0",
                    AgentID                   = "0",
                    QuotationPolicyID         = "0",
                    IsDriverAgeLoadingApplied = false, 
					QuotationFlexFields = generalRequest["QuotationFlexFields"]?.Type == JTokenType.Array
	? generalRequest["QuotationFlexFields"].ToObject<List<QuotationFlexFieldViewModel>>()
	: new() 
					//QuotationFlexFields       = new()
					//{
					//    new QuotationFlexField
					//    {
					//        ColumnID      = "1402",
					//        FieldValue    = "JO",
					//        SubjectSerial = null,
					//        DriverID      = "0"
					//    }
					//}
				}
            };

            return await PostAsync<object>(
                $"{Settings.GeneralBaseUrl}/api/Quotation/UpdateQuotation", payload);
        }

        public async Task InsertAttachments(List<IdFiles> files, string documentId)
        {
			List<IdFiles> filesArray = files;
            if (filesArray == null || !filesArray.Any()) return;

            var firstFile = filesArray.FirstOrDefault();
            if (firstFile == null) return;

            var payload = new AttachmentRequest
            {
                GeneralAttachment = new GeneralAttachment
                {
                    CreatedBy       = Settings.DefaultUser,
                    DocumentType    = firstFile.documentType,
                    Path            = firstFile.name,
                    PolicyNo        = documentId,
                    ReceivedDate    = DateTime.Now.ToString(),
                    Remarks         = "Remarks",
                    Serial          = "2",
                    Id              = "0",
                    AttachmentBytes = firstFile.base64,
                }
            };

            await PostAsync<object>(
                $"{Settings.GeneralBaseUrl}/api/Quotation/InsertAttachment", payload);
        }


		public async Task DeleteAttachment( string documentId)
		{
		 

			var payload = new DeleteAttachment
			{
				 ID = documentId,
			};

			await PostAsync(
				$"{Settings.GeneralBaseUrl}/api/Quotation/DeleteAttachment", payload);
		}


		public async Task InsertQuotationInterest(
            string documentId, long customerId, JObject cart, JObject generalRequest)
        {
            var now = DateTime.Now;
            var expiryDate = generalRequest.GetDateSafe("ExpiryDate")    ?? now;
            var effectiveDate = generalRequest.GetDateSafe("EffectiveDate") ?? now;

            var birthDate = generalRequest.GetDateSafe("BirthDate")
                         ?? cart.GetDateSafe("BirthDate")
                         ?? cart.GetDateSafe("personalInfo.dateOfBirth")
                         ?? cart.GetDateSafe("tripDetails.dateOfBirth");

            var interestType = generalRequest.GetStringSafe("InterestType")
                            ?? cart.GetStringSafe("InterestType")
                            ?? cart.GetStringSafe("tripDetails.InterestType")
                            ?? "1102";

            var nationalId = cart.GetStringSafe("GaNationalId", "NationalId", "NationalID");
            var interestName = cart.GetStringSafe("InterestName", "InsuredName", "OwnerName");

            var payload = new InsertQuotationInterest
            {
                GeneralInterest = new GeneralInterest
                {
                    UnitSumInsured    = "1",
                    SumInsured        = generalRequest.GetStringSafe("SumInsured") ?? "0",
                    PolicyTypeText    = "Travel Insurance",
                    PolicyType        = generalRequest.GetStringSafe("PolicyType") ?? cart.GetStringSafe("PolicyType"),
                    PlateType         = "0",
                    OwnerName         = interestName,
                    MinExcessAmount   = "0",
                    LiabilityLimit    = "7",
                    IsTopLocation     = "0",
                    InterestName      = interestName,
                    InterestName2     = interestName,
                    Nationality       = cart.GetStringSafe("personalInfo.nationalityCode") ?? "JO",
                    InterestType      = interestType,
                    InterestSerial    = "2",
                    GaNationalId      = nationalId,
                    ExpiryDate        = expiryDate,
                    ExcessFrom        = "2",
                    ExchangeRate      = "1",
                    EffectiveDate     = effectiveDate,
                    DocumentID        = documentId,
                    CurrencyCode      = Settings.DefaultCurrency,
                    Quantity          = "1",
                    CreatedBy         = "ONLINE",
                    Jurisdiction      = "23041",
                    IsDriverCovered   = "0",
                    Country           = "JO",
                    ClassID           = generalRequest.GetStringSafe("ClassID") ?? cart.GetStringSafe("ClassID"),
                    BirthDate         = birthDate,
                    BeneficiaryID     = customerId.ToString(),
                    CoveredCountries  = new()
                    {
                        new() { Country = "JO", DateFrom = effectiveDate, DateTo = expiryDate }
                    },
					IntrestFlexFields = generalRequest["IntrestFlexFields"]?.Type == JTokenType.Array
	? generalRequest["IntrestFlexFields"].ToObject<List<QuotationFlexFieldViewModel>>()
	: new ()

				}
            };
            await PostAsync<object>(
                $"{Settings.GeneralBaseUrl}/api/Quotation/InsertQuotationInterest", payload);
        }

        public async Task InsertQuotationCovers(string documentId) =>
            await PostAsync<object>(
                $"{Settings.GeneralBaseUrl}/api/QUOTATION/INSERTQUOTATIONCOVERS",
                new InsertQuotationCovers { QuotationID = documentId });

        public async Task CalculateQuotation(string documentId) =>
            await PostAsync<object>(
                $"{Settings.GeneralBaseUrl}/api/QUOTATION/CALCULATEQUOTATION",
                new { DocumentID = documentId, CheckForApproval = true, GetClausesOnCalcuate = true });

        public async Task CalculateQuotationFinal(string documentId) =>
            await PostAsync<object>(
                $"{Settings.GeneralBaseUrl}/api/QUOTATION/CALCULATEQUOTATION",
                new { DocumentID = documentId, CheckForApproval = false });

        public async Task CheckApproval(string documentId) =>
            await PostAsync<object>(
                $"{Settings.GeneralBaseUrl}/api/POLICY/CHECKFORAPPROVALNOTIFICATION",
                new { DocumentID = documentId, UserName = Settings.DefaultUser });

        public async Task UpdateQuotationStatus(string documentId) =>
            await PostAsync<object>(
                $"{Settings.GeneralBaseUrl}/api/Quotation/UpdateQuotationStatus",
                new { PolicyID = documentId, NewQuotationStatus = 8, ModifiedBy = Settings.DefaultUser });

        public async Task<ConvertToPolicyResponse> ConvertToPolicy(string documentId) =>
            await PostAsync<ConvertToPolicyResponse>(
                $"{Settings.GeneralBaseUrl}/api/QUOTATION/CONVERTTOPOLICY",
                new { QuotationId = documentId, FlagAttachmentsAsReceived = true, UserName = Settings.DefaultUser });

    }
}