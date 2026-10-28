using Elastic.Clients.Elasticsearch.Snapshot;
using Google.Apis.Storage.v1.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace EskaCMS.Integrations.Models.Insurance
{
    // ========================= ATTACHMENTS =========================

    public class AttachmentRequest
    {
        public GeneralAttachment GeneralAttachment { get; set; }
    }


    public class GeneralAttachment
    {

        public string CreatedBy { get; set; } = "ONLINE";
        public string ModifiedBy { get; set; } = null;
        public string DocumentType { get; set; }
        public int? IsReceived { get; set; } = null;
        public string Path { get; set; }
        public string PolicyNo { get; set; }
        public string ReceivedDate { get; set; }
        public string Remarks { get; set; } = "Remarks";
        public string Serial { get; set; } = "2";
        public string Id { get; set; } = "0";
        public string AttachmentBytes { get; set; } // Base64
    }


    public class FileUploadRequest
    {
        public List<FileItem> idFiles { get; set; } = new();
    }

    public class FileItem
    {
        public string name { get; set; }
        public string type { get; set; }
        public string size { get; set; }
        public string documentType { get; set; }
        public string base64 { get; set; }
    }

    // ========================= CALCULATE / APPROVAL =========================

    public class CalculateQuotation
    {
        public bool CheckForApproval { get; set; } = true;
        public bool GetClausesOnCalcuate { get; set; } = true;
        public long DocumentID { get; set; }
    }

    public class CheckApproval
    {
        public string UserName { get; set; } = "ONLINE";
        public long DocumentId { get; set; }
    }

    public class CHECKFORAPPROVALNOTIFICATION
    {
        public string UserName { get; set; } = "ONLINE";
        public long DocumentID { get; set; }
    }

    public class ConvertToPolicy
    {
        public long QuotationId { get; set; }
        public bool FlagAttachmentsAsReceived { get; set; } = true;
        public string UserName { get; set; } = "ONLINE";
    }

    // ========================= RECEIPT =========================

    public class GenerateReceipt
    {
        public int BankAccountID { get; set; } = 931431;
        public decimal? BankAmount { get; set; } = null;

        public int CommissionAccountID { get; set; } = 931432;
        public decimal CommissionAmount { get; set; } = 0;

        public DateTime? ReceiptDate { get; set; } = null;

        public long PolicyID { get; set; }
        public string TransactionIDs { get; set; }

        public int ApplicationID { get; set; } = 8;
        public int CostCenterId { get; set; } = 0;

        public string Note { get; set; } = "-1";
        public string ApprovalNo { get; set; } = "";
        public string CreditCardNo { get; set; } = "";
        public string ReferenceNo { get; set; } = "";
    }

    // ========================= QUOTATION =========================

    public class UpdateQuotation
    {
        public GeneralQuotation GeneralQuotation { get; set; } = new();
    }

    public class GeneralQuotation
    {
        public string CurrencyCode { get; set; } = "JOD";
        public string ExchangeRate { get; set; } = "1.0";

        public DateTime IssueDate { get; set; } = DateTime.Now;
        public DateTime EffectiveDate { get; set; } = DateTime.Now;
        public DateTime ExpiryDate { get; set; }
        public string ModifiedBy { get; set; }
        public string AccountedFor { get; set; } = "1";
        public string OpenCoverType { get; set; } = "0";

        public string CreatedBy { get; set; } = "ONLINE";

        public string Insured { get; set; }
        public string NationalID { get; set; }
        public string InsuredName { get; set; }
        public string InsuredAddress { get; set; } = "Jordan-Amman";

        public string PaymentCycleID { get; set; } = "1";
        public string ClassID { get; set; }
        public string BusinessType { get; set; } = "1";
        public string PolicyType { get; set; }
        public string PolicyDays { get; set; }

        public string BranchID { get; set; } = "9";
        public string CompanyID { get; set; } = "1";
        public string FiscalYear { get; set; } = DateTime.Now.Year.ToString();
        public string CalculationBase { get; set; } = "4";

        public DateTime QuotationDate { get; set; } = DateTime.Now;
        public DateTime QuotationExpiryDate { get; set; }
        public DateTime VoucherDate { get; set; } = DateTime.Now;

        public string QuotationValidity { get; set; } = "90";
        public string QuotationType { get; set; } = "2";
        public string AgentType { get; set; } = "1";

        public string BranchCurrency { get; set; } = "JOD";
        public string BranchExrate { get; set; } = "1.0";

        public string DocumentID { get; set; } = "0";

        public string IsPrinted { get; set; } = "0";
        public string IsReinsured { get; set; } = "0";
        public string IsConverted { get; set; } = "0";
        public bool IsUpdateForConvert { get; set; } = false;

        public string PrintCount { get; set; } = "0";
        public string SalesType { get; set; } = "0";
        public string Beneficiary { get; set; }

        public string IsCalculated { get; set; } = "0";
        public string DocumentNo { get; set; } = "0";

        public string NetPremium { get; set; } = "0";
        public string GrossPremium { get; set; } = "0";
        public string TotalFees { get; set; } = "0";
        public string CommissionAmount { get; set; } = "0";
        public string ExcessAmount { get; set; } = "0";
        public string LiabilityLimit { get; set; } = "0";

        public bool IsDriverAgeLoadingApplied { get; set; } = false;

        public string TotalInstallmentsAmount { get; set; } = "0";
        public string IntLoadingSurvayId { get; set; } = "0";
        public string ProspectLeadLocation { get; set; } = "0";
        public string AgentID { get; set; } = "0";
        public string QuotationPolicyID { get; set; } = "0";
        public string OurSharePercentage { get; set; } = "0";
        public string AgtCommissionAmount { get; set; } = "0";

        public List<QuotationFlexFieldViewModel> QuotationFlexFields { get; set; }
    
    }

    public class QuotationFlexField
    {
        public string FieldValue { get; set; }
        public string SubjectSerial { get; set; }
        public string ColumnID { get; set; }
        public string DriverID { get; set; }
    }

    public class InsertQuotationCovers
    {
        public string QuotationID { get; set; }
    }

    // ========================= INTEREST =========================

    public class InsertQuotationInterest
    {
        public GeneralInterest GeneralInterest { get; set; } = new();
    }
	public class QuotationFlexFieldViewModel
	{
		public string? ColumnID { get; set; }
		public string? DriverID { get; set; }
		public string? FieldValue { get; set; }
		public string? SubjectSerial { get; set; }
	}
	public class GeneralInterest
    {
        public string UnitSumInsured { get; set; } = "1";
        public string SumInsured { get; set; } = "1";

        public string PolicyTypeText { get; set; } = "Personal Accident";
        public string PolicyType { get; set; }

        public string PlateType { get; set; } = "0";
        public string OwnerName { get; set; }

        public string Notes { get; set; } = null;
        public string MinExcessAmount { get; set; } = "0";
        public string LiabilityStructID { get; set; } = null;
        public string LiabilityLimit { get; set; } = "7";
        public string IsTopLocation { get; set; } = "0";

        public string InterestName { get; set; }
        public string InterestName2 { get; set; }

        public string Nationality { get; set; } = "JO";
        public string InterestType { get; set; } = "1102";
        public string InterestSerial { get; set; } = "2";

        public string GaNationalId { get; set; }
        public DateTime ExpiryDate { get; set; }

        public string ExcessFrom { get; set; } = "2";
        public string ExchangeRate { get; set; } = "1";

        public DateTime EffectiveDate { get; set; } = DateTime.Now;
        public string DocumentID { get; set; }

        public string CurrencyCode { get; set; } = "JOD";
        public string Quantity { get; set; } = "1";
        public string CreatedBy { get; set; } = "ONLINE";

        public string Jurisdiction { get; set; } = "23041";
        public string IsDriverCovered { get; set; } = "0";
        public string Country { get; set; } = "JO";
        public string ClassID { get; set; }

        public DateTime? BirthDate { get; set; }
        public string BeneficiaryID { get; set; }

        public List<CoveredCountry> CoveredCountries { get; set; } = new();
        public List<QuotationFlexFieldViewModel> IntrestFlexFields { get; set; }

    }

    public class CoveredCountry
    {
        public string Country { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
    }

    public class InterestFlexField
    {
        public string FieldValue { get; set; }
        public string ColumnID { get; set; }
    }

    // ========================= STATUS =========================

    public class UpdateQuotationStatus
    {
        public long PolicyId { get; set; }
        public int NewQuotationStatus { get; set; }
        public string ModifiedBy { get; set; } = "ONLINE";
    }

    // ========================= SETTINGS =========================

    public class IntegrationSettings
    {
        public string GeneralBaseUrl { get; set; }
        public string MedicalBaseUrl { get; set; }

        public string FinancialBaseUrl { get; set; }
        public string FinancialBaseUrl2 { get; set; }

        public string LongLifeToken { get; set; }

        public string DefaultUser { get; set; } = "ONLINE";
        public string DefaultCurrency { get; set; } = "JOD";

        public int CompanyId { get; set; } = 1;
        public int BranchId { get; set; } = 9;

        public ReceiptSettings Receipt { get; set; }
    }

    public class ReceiptSettings
    {
        public string BankAccountId { get; set; }
        public string BankAmount { get; set; }
        public string CommissionAccountId { get; set; }
        public string CreditCardCode { get; set; }
        public string ApplicationId { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public string ApprovalNo { get; set; }
        public string CreditCardNo { get; set; }
        public string ApplicationID { get; set; }
        public string ReferenceNo { get; set; }
        public string CommissionAmount { get; set; }
        public string CostCenterId { get; set; }
        public string Note { get; set; }
    }



    public class ConvertToPolicyResponse
    {
        public GeneralPolicy GeneralPolicy { get; set; }
        public Status Status { get; set; }
        public int? TransactionId { get; set; }

    }
    public class GeneralPolicy
    {
        public decimal? NetPremium { get; set; }
        public int? DocumentNo { get; set; }
		public int? DocumentID { get; set; }

		public string SegmentCode { get; set; }
		 



	}
    public class Status
    {
        public int StatusCode { get; set; }
        public string Reason { get; set; }


    }

    public class UpdatePolicy
    {


        public string CreatedBy { get; set; }
        public string PolicyID { get; set; }   // QuotationId
        public string ModifiedBy { get; set; }

        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }

        public string DocumentType { get; set; }
        public string PolicyType { get; set; }

        public string APPLICANT_NAME { get; set; }

        public string UWYear { get; set; }
        public string APPLICANT_TYPE { get; set; }

        public DateTime ReferenceDate { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime EffectiveDate { get; set; }

        public string BusinessType { get; set; }
        public string QuotationType { get; set; }
        public string CalculationType { get; set; }
        public string FamilyClassFlag { get; set; }

        public string CRM_LED_ID { get; set; } = "0";

        public string Exrate { get; set; }

        public string APPROVAL_STATUS { get; set; }
        public string EndorsementID { get; set; }
        public string PaymentID { get; set; }

        public string PlanID { get; set; }
        public string PolicyHolderID { get; set; }  // CustomerId

        public string CurrencyCode { get; set; }

        public string CompanyID { get; set; }
        public string BranchID { get; set; }

        public string ReferenceNo { get; set; }

        public string TpaType { get; set; }

        public DateTime ReceviedDate { get; set; }

        public string AccountedFor { get; set; }

        public string DURATION { get; set; }
        public string DURATION_UNIT { get; set; }

        public string MPD_APP_ID { get; set; }

        public string ProductID { get; set; }

        [JsonProperty("Discount%")]
        public string DiscountPercent { get; set; }
    }

    public class ConvertQuotationART
    {
        public string Created_by { get; set; }
        public string ConvertMembers { get; set; }
        public string ID { get; set; }
    }

    public class PostPolicy
    {
        public int ID { get; set; }   // policy id

        public string lcFeesId { get; set; }

        public string lcFeesPercentage { get; set; }

        public string CREATED_BY { get; set; }
    }


    public class InitializeNotificatoin
    {
        public int PolicyID { get; set; }   // policy id
    }
    public class OrderCartContext
    {
        public JObject Root { get; set; }               // normalized cart/root
        public JObject QuotationResponse { get; set; }  // parsed quotationResponse object
        public JObject GeneralRequest { get; set; }     // unified object used by flow methods
        public string ProductCode { get; set; }
        public string NationalId { get; set; }
        public string DocumentId { get; set; }
        public string BirthDate { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Name { get; set; }
    }
    public class ConvertQuotationARTResponseVm
    {
        public StatusVm Status { get; set; }
        public PolicyVm oPolicy { get; set; }
        public int? DebitNoteID { get; set; }
        public int PolicyID { get; set; }   // Root PolicyID (currently 0)
    }
    public class StatusVm
    {
        public int StatusCode { get; set; }
        public string Reason { get; set; }
        public DateTime ReasonDate { get; set; }
    }

    public class DeleteAttachment {
		public string ID { get; set; }
	}
    public class PolicyVm
    {
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }
        public DateTime FINANCIAL_DATE { get; set; }

        public string CONTACT_PERSON { get; set; }
        public int? POLICY_HOLDER_TYPE { get; set; }
        public string CCHI_POLICY_NO { get; set; }

        public int? CRM_LED_ID { get; set; }
        public string PRODUCT_NAME { get; set; }

        public int? IN_UTILIZATION { get; set; }
        public int? OUT_UTILIZATION { get; set; }

        public string PaymentName { get; set; }
        public string EndorsementName { get; set; }
        public string PlanName { get; set; }

        public int IS_CONVERTED { get; set; }
        public string BranchName { get; set; }
        public string QuotationStatusName { get; set; }
        public string CRM_LEAD_NAME { get; set; }

        public int APPROVAL_STATUS { get; set; }
        public string APPROVAL_STATUS_DESC { get; set; }

        public string E_FORMS_CLASS_TYPE { get; set; }
        public int? APP_UTILIZATION { get; set; }

        public string SPOC_POLICY_HOLDER { get; set; }
        public string SPOC_EMAIL { get; set; }
        public string SPOC_CONTRACT_NO { get; set; }

        public string SUB_ENDT_TYPE_DESC { get; set; }

        public int DocumentType { get; set; }
        public double PolicyNo { get; set; }

        public int PolicyType { get; set; }
        public string PolicyTypeDesc { get; set; }

        public string AgentName { get; set; }
        public string RenewalNote { get; set; }

        public int RenewalNo { get; set; }
        public int EndorsementNo { get; set; }
        public int UWYear { get; set; }

        public string ReferenceNo { get; set; }
        public DateTime ReferenceDate { get; set; }

        public DateTime IssueDate { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpiryDate { get; set; }

        public int BusinessType { get; set; }
        public int CalculationType { get; set; }
        public int FamilyClassFlag { get; set; }

        public decimal? ExtraNRPPercent { get; set; }
        public decimal? LoadingPercent { get; set; }
        public decimal? DiscountPercent { get; set; }

        public decimal DISCOUNT_AMOUNT { get; set; }
        public string Notes { get; set; }

        public int IS_CALCULATED { get; set; }
        public decimal Exrate { get; set; }

        public int PaymentID { get; set; }
        public int EndorsementID { get; set; }
        public int PlanID { get; set; }

        public int? OriginalPolicyID { get; set; }
        public int? RenewedPolicyID { get; set; }

        public int QuotationID { get; set; }
        public int PolicyHolderID { get; set; }

        public string CurrencyCode { get; set; }
        public int CompanyID { get; set; }
        public int BranchID { get; set; }
        public int TpaType { get; set; }

        public DateTime ReceviedDate { get; set; }
        public string SalesCollector { get; set; }

        public int AccountedFor { get; set; }
        public int? AccountID { get; set; }
        public int? AccountedTo { get; set; }

        public int? QuotationType { get; set; }

        public int APPLICANT_TYPE { get; set; }
        public string APPLICANT_NAME { get; set; }

        public int DURATION { get; set; }
        public decimal TOTAL_FEES { get; set; }
        public int DURATION_UNIT { get; set; }

        public int PolicyID { get; set; }
        public string TERMINATION_REASON { get; set; }
        public string TERMINATION_DETAILS { get; set; }

        public string SegmentCode { get; set; }

        public string BusinessChannel { get; set; }

        public decimal NetPremium { get; set; }
        public string MPD_APP_ID { get; set; }
        public decimal NetPremiumLC { get; set; }

        public decimal GrossPremium { get; set; }
        public decimal GrossPremiumLC { get; set; }

        public decimal TotalPremium { get; set; }
        public decimal TotalPremiumLC { get; set; }

        public decimal AGT_COMM_PER { get; set; }

        public int ProductID { get; set; }
        public string PROMOTION_CODE { get; set; }

        public int SubEndorsementID { get; set; }
        public int? CONVERT_POLICY_ID { get; set; }

        public int IS_POSTED { get; set; }
        public string PaymentMethod { get; set; }

        public decimal LOADING_AMT { get; set; }
        public string SadadNo { get; set; }
    }
    public class CustomerInfoInsuranceViewModel
    {
        public string Phone { get; set; }
        public string NationalId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }


    public class TransactionResponse
    {


        public long ID { get; set; }


    }



	public class IdFiles
	{
		public string name { get; set; }
		public string fileName { get; set; }
		public string type { get; set; }
		public long? size { get; set; }
		public long? lastModified { get; set; }
		public string documentType { get; set; }
		public string side { get; set; }
		public string base64 { get; set; }
	}
    public class CalculateQoutationVM
    {
		public List<IdFiles> Files { get; set; }
		public long cartId { get; set; }


	}

    public class RepushPoliciesVM
    {
        public List<long> OrderIds { get; set; }
    }

    public class RepushResultVM
    {
        public long OrderId { get; set; }
        public bool Success { get; set; }
        // What happened: Created | MappedExisting | AlreadyMapped | SkippedNotPaid | OrderNotFound | Error
        public string Action { get; set; }
        public string Error { get; set; }
    }


}



