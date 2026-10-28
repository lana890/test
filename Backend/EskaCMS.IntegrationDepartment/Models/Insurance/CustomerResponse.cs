using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Integrations.Models.Insurance
{
   

		public class CustomerResponse
		{
			public List<CustomerData> Data { get; set; }
			public string ErrorMessage { get; set; }
			public string SuccessCollection { get; set; }
			public string ErrorInnerMessage { get; set; }
			public string ErrorCode { get; set; }
			public bool IsError { get; set; }
			public string TotalRecords { get; set; }
		}

		public class CustomerData
		{
			public int ID { get; set; }
			public string CustomerNo { get; set; }
			public string Name { get; set; }
			public string Name2 { get; set; }
			public string NationalId { get; set; }
			//public string CommercialName { get; set; }
			//public string COM_REG_ID { get; set; }
			//public int? CustomerType { get; set; }
			//public DateTime BirthDate { get; set; }
			//public string Nationality { get; set; }
			//public int? Category { get; set; }
			//public string CityCode { get; set; }
			//public string CountryCode { get; set; }
			//public string Mobile { get; set; }
			//public string Currency { get; set; }
			//public string Email { get; set; }
			//public DateTime StartDate { get; set; }
			//public DateTime ExpiryDate { get; set; }
			//public int? BlackList { get; set; }
			//public string BlackListReason { get; set; }
			//public int OnHold { get; set; }
			//public string HoldReason { get; set; }
			//public int? CompanyId { get; set; }
			//public int? BranchId { get; set; }
			//public string CreatedBy { get; set; }
			//public DateTime CreationDate { get; set; }
			//public DateTime ModificationDate { get; set; }
			//public string ModifiedBy { get; set; }
			//public string Phone { get; set; }
			//public string MainAddress { get; set; }
			//public string Fax { get; set; }
			//public int? Taxable { get; set; }
			//public double TaxPercentage { get; set; }
			//public string StreetName { get; set; }
			//public string BuildingNo { get; set; }
			//public string Website { get; set; }
			//public int? CST_SUP_FLAG { get; set; }
			//public string RoleID { get; set; }
			//public string RoleIDs { get; set; }
			//public int? ParentId { get; set; }
			//public double? Credit_limit { get; set; }
			//public bool? IsCustomer { get; set; }
			//public List<int> DR_CustomerAccounts { get; set; }
			//public List<int> CR_CustomerAccounts { get; set; }
			//public string LICENSE_NO { get; set; }
			//public List<int> CUSTOMERS_TYPES { get; set; }
			//public CustomerExtend Extend { get; set; }
			//public object Contacts { get; set; }
			//public object LocalForeign { get; set; }
			//public object CustomerBasicInfo { get; set; }
		}

		public class CustomerExtend
		{
			public string AML_RISK_RATING { get; set; }
			public string BankAccount { get; set; }
			public string BeneficiaryName { get; set; }
			public int? BlackListCode { get; set; }
			public string Block { get; set; }
			public string BusAdditionalNo { get; set; }
			public string BusBuildingNo { get; set; }
			public string BusCity { get; set; }
			public string BusDistrict { get; set; }
			public string BusPostCode { get; set; }
			public string BusStreetName { get; set; }
			public string Channel { get; set; }
			public double? ChequesLimit { get; set; }
			public string ComplianceNotes { get; set; }
			public int? COMP_INDV_FLAG { get; set; }
			public string ContactPerson { get; set; }
			public string CountryOfIncorporation { get; set; }
			public double CreditLimit { get; set; }
			public double NewCreditLimit { get; set; }
			public int? CRG_ARA_CODE { get; set; }
			public int? CompanyId2 { get; set; }
			public string CurrencyCode { get; set; }
			public double? CustomerRate { get; set; }
			public DateTime DateOfExcuse { get; set; }
			public int? EconomySector { get; set; }
			public string Email2 { get; set; }
			public int? EmailNotification { get; set; }
			public int? Employer { get; set; }
			public int? FamilyNo { get; set; }
			public int? FCR_PTM_ID { get; set; }
			public int? CostCenterId { get; set; }
			public string FirstName { get; set; }
			public string FirstNameArabic { get; set; }
			public int? Frequency { get; set; }
			public int? Gender { get; set; }
			public int? HoldCode { get; set; }
			public int? HomeNotification { get; set; }
			public DateTime IncorporationDate { get; set; }
			public int? IndividualOrCompany { get; set; }
			public string Interface { get; set; }
			public int? IsCollector { get; set; }
			public int? IsDelete { get; set; }
			public int? IsLegal { get; set; }
			public int? IsRelated { get; set; }
			public int? IsVIP { get; set; }
			public int? Language { get; set; }
			public string LastName { get; set; }
			public string LastNameArabic { get; set; }
			public string LegalForm { get; set; }
			public string LineOfBusiness { get; set; }
			public int? LocalOrForeign { get; set; }
			public string MailingAddress { get; set; }
			public string ManagerId { get; set; }
			public string ManageName { get; set; }
			public int? MaritalStatus { get; set; }
			public string MiddleName { get; set; }
			public string MiddleNameArabic { get; set; }
			public string Mobile2 { get; set; }
			public string NextOfKinAddress { get; set; }
			public string NextOfKinName { get; set; }
			public string Notes { get; set; }
			public int? NoOfEmployees { get; set; }
			public int? NoOutstanding_INV { get; set; }
			public int NumberOfChildren { get; set; }
			public string OfficeAddress { get; set; }
			public string Passport { get; set; }
			public string Phone2 { get; set; }
			public string PlanOfTaxation { get; set; }
			public string Pobox { get; set; }
			public string PostalCode { get; set; }
			public int? PostalNotification { get; set; }
			public string PrincipalCity { get; set; }
			public string Product { get; set; }
			public int? Profession { get; set; }
			public DateTime ResidentialDate { get; set; }
			public int? ResidentFlag { get; set; }
			public double? Revenues { get; set; }
			public string RiskLevel { get; set; }
			public string Road { get; set; }
			public int? SMS_Notification { get; set; }
			public string SourceOfIncome { get; set; }
			public int? SubEconomySector { get; set; }
			public string TaxCenter { get; set; }
			public int? TaxCenterCode { get; set; }
			public string TaxNo { get; set; }
			public string TaxTakingProperty { get; set; }
			public int? Title { get; set; }
			public string TradingAddress { get; set; }
			public string EconomySectorName { get; set; }
			public string EconomySectorName2 { get; set; }
		}



	public class InsertCustomerRequest
	{

            public int CompanyId { get; set; }
            public int CompanyId2 { get; set; }
            public int BranchId { get; set; }

            public string NationalId { get; set; }
            public string Name { get; set; }

            public string Phone { get; set; }
            public string Email { get; set; }
            public int TAXABLE { get; set; }
		public string MainAddress { get; set; }

		public string CountryCode { get; set; }
            public string Nationality { get; set; }

            public string CreatedBy { get; set; }

            public int BlackList { get; set; }
            public int CityCode { get; set; }
            public int CST_SUP_FLAG { get; set; }

            public int IndividualOrCompany { get; set; }
            public int LocalOrForeign { get; set; }

            public int IsDelete { get; set; }
            public int OnHold { get; set; }

            public int Language { get; set; }
            public int Gender { get; set; }
            public int Title { get; set; }

            // Dates are string because Financial API expects string format
            public string StartDate { get; set; }
            public string CreationDate { get; set; }

		public int CUSTOMERTYPE { get; set; }
	}

	public class UpdateCustomerRequest
	{
        public int ID { get; set; }
        public int CompanyId { get; set; }
        public int BranchId { get; set; }
        public int TAXABLE { get; set; }
        public int COMP_INDV_FLAG { get; set; }
		public string MainAddress { get; set; }
		public string NationalId { get; set; }
        public string Name { get; set; }

        public string Phone { get; set; }
        public string Email { get; set; }

        public string CountryCode { get; set; }
        public string Nationality { get; set; }

        public string CreatedBy { get; set; }
		public int BlackList { get; set; }
        public int CityCode { get; set; }
        public int CST_SUP_FLAG { get; set; }

        public int IndividualOrCompany { get; set; }
        public int LocalOrForeign { get; set; }

        public int IsDelete { get; set; }
        public int OnHold { get; set; }

        public int Language { get; set; }
        public int Gender { get; set; }
        public int Title { get; set; }

        // Dates are string because Financial API expects string format
        public string StartDate { get; set; }
        public string CreationDate { get; set; }
        public int CRG_COM_ID2 { get;  set; }
        public int CompanyId2 { get;  set; }
        public string SEGMENT_CODE { get; internal set; }

		public int CUSTOMERTYPE { get; set; }
	}
}
