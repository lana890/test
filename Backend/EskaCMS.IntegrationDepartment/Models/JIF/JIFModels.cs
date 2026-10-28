using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EskaCMS.Integrations.Models.JIF
{
	public class JIFModels
	{

	}
	public class JifLoginRequest
	{
		public string USER_ID { get; set; }
		public string PASSWORD { get; set; }
	}
	public class JifLoginResponse
	{
		public int ResponseCode { get; set; }
		public string ResponseMessage { get; set; }
		public string Token { get; set; }
	}

	public class JIFSettings
	{

		public string baseURL { get; set; }
		public string USER_ID { get; set; }

		public string PASSWORD { get; set; }
		public string CachePeriod { get; set; }


	}




	public class VehicleInquiryViewModel : IValidatableObject
	{
		// Vehicle Info
		public string? PLATE_NUM { get; set; }
		public string? PLATE_DIST { get; set; }
		public string? REGISTRATION_NUMBER { get; set; }
		public string? CHASE_NUM { get; set; }

		// Optional Flags
		public int? TENDER_FLAG { get; set; } // 1 or null

		// Authentication
		public string? USER_ID { get; set; }
		public string? PASSWORD { get; set; }

		// Tender Info
		public string? TENDER_NUMBER { get; set; }

		// Owner Info
		public string? FIRST_NAME { get; set; }
		public string? FATHER_NAME { get; set; }
		[JsonProperty("GRFATHER_NAME")]
		public string? GRANDFATHER_NAME { get; set; }
		public string? FAMILY_NAME { get; set; }
		public long? OWNER_NATIONAL_ID { get; set; }

		// Establishment Info
		public string? ESTABLISHMENT_NAME { get; set; }
		public long? ESTABLISHMENT_NATIONAL_ID { get; set; }



		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			bool hasChassis = !string.IsNullOrWhiteSpace(CHASE_NUM);

			bool hasPlateInfo =
				!string.IsNullOrWhiteSpace(PLATE_NUM) &&
				!string.IsNullOrWhiteSpace(PLATE_DIST) &&
				!string.IsNullOrWhiteSpace(REGISTRATION_NUMBER);

			// ❌ If neither condition is satisfied → invalid
			if (!hasChassis && !hasPlateInfo)
			{
				yield return new ValidationResult(
					"You must provide either CHASE_NUM OR (PLATE_NUM + PLATE_DIST + REGISTRATION_NUMBER).",
					new[]
					{
					nameof(CHASE_NUM),
					nameof(PLATE_NUM),
					nameof(PLATE_DIST),
					nameof(REGISTRATION_NUMBER)
					}
				);
			}



		}

	}

		public class VehicleResponseViewModel
		{
			public string? PLATE_NUM { get; set; }
			public string? PLATE_DIST { get; set; }
			public string? CHASE_NUM { get; set; }
			public string? REGISTRATION_NUMBER { get; set; }

			public DateTime? DVLD_EXP_DATE { get; set; }
			public string? VEHICLE_COLOR { get; set; }

			public string? OWNER_NAME { get; set; }
			public string? OWNER_ADDRESS { get; set; }
			public string? OWNER_PHONE_NUMBER { get; set; }

			public string? COMPENSATION_AMOUNT { get; set; }
			public string? TKT_FLAG { get; set; }

			public Guid? REQUEST_ID { get; set; }

			public string? REGISTRATION_TYPE { get; set; }
			public string? USAGE_TYPE { get; set; }

			public string? CATEGORY_TYPE { get; set; }
			public string? MANUFACTURING_YEAR { get; set; }

			public string? VEHICLE_TYPE { get; set; }
			public string? VEHICLE_CATEGORY { get; set; }

			public string? WEIGHT { get; set; }
			public string? CARGO_DESC { get; set; }

			public string? ENGINE_NUMBER { get; set; }
			public string? ENGINE_CAPACITY { get; set; }

			public string? PASSENGERS_COUNT { get; set; }

			public DateTime? INSUR_END_DATE { get; set; }

			public string? NATIONAL_ID { get; set; }

			public string? NBVEH_MAIN_COLOR_C13 { get; set; }
			public string? NBVEH_VEH_CLASS_C09 { get; set; }

			public string? DESC_VEH_CLASS { get; set; }

			public string? NBVEH_RGST_TYPE_C03_DESC { get; set; }
			public string? NBVEH_CARGO_DES_C10 { get; set; }

			public string? INSUR_NUM { get; set; }
			public string? COMP_INUSR { get; set; }
			public string? INSUR_TYPE { get; set; }

			public string? FUEL_TYPE { get; set; }
			public string? CARGO_TYPE { get; set; }

			public string? PARTNER_ADJECTIVE { get; set; }

			public string? MOBILE_1 { get; set; }
			public string? MOBILE_2 { get; set; }

			public string? PARTNER_ADDRESS { get; set; }

			public string? NBVEH_CTG_C08 { get; set; }
			public string? NBVEH_CTG_C07 { get; set; }
		}
	public class PolicyRequestViewModel
	{
		// Vehicle Info
		public string? PLATE_NUM { get; set; }
		public string? PLATE_DIST { get; set; }
		public string? CHASE_NUM { get; set; }
		public string? REGISTRATION_NUMBER { get; set; }

		public int? TENDER_FLAG { get; set; }

		// Authentication (Mandatory)
		public string USER_ID { get; set; } = null!;
		public string PASSWORD { get; set; } = null!;

		// Transaction (Mandatory)
		public string PERIOD_ID { get; set; } = null!;
		public string REQUEST_ID { get; set; }

		// Tax / Stamp Flags
		public bool TAX_EXEMPTION_FLAG { get; set; }
		public bool STAMP_EXEMPTION_FLAG { get; set; }

		public string? TAX_EXEMPTION_BOOK { get; set; }
		public int? TAX_EXEMPTION_SOURCE { get; set; }

		public bool? APPLICATION_FLAG { get; set; }
		public bool? BUS_COMPLEX_FLAG { get; set; }

		// Contact (Mandatory)
		public string MOBILE_NUMBER { get; set; } = null!;

		// Insurance Timing
		public bool ISSUE_FROM_DATE_FLAG { get; set; }
		public string? MANUAL_INSURANCE_START_DATE { get; set; }

		// Vehicle Usage (Mandatory)
		public int VEHICLE_USAGE_TYPE { get; set; }
		public int VEHICLE_CARGO_TYPE { get; set; }

		// Manual Info
		public string? MANUAL_INSURED_NAME { get; set; }

		public string STAFF_NAME { get; set; } = null!;
	}

	public class PolicyResponseViewModel
	{

		// Vehicle Info
		public string? PLATE_NUM { get; set; }
		public string? PLATE_DIST { get; set; }
		public string? CHASE_NUM { get; set; }
		public string? REGISTRATION_NUMBER { get; set; }

		// Dates
		public string? DVLD_EXP_DATE { get; set; }
		public string? INSURANCE_START_DATE { get; set; }
		public string? INSURANCE_END_DATE { get; set; }

		// Vehicle Info
		public string? VEHICLE_COLOR { get; set; }

		// Owner Info
		public string? OWNER_NAME { get; set; }
		public string? OWNER_ADDRESS { get; set; }
		public string? OWNER_PHONE_NUMBER { get; set; }

		// Flags
		public string? TKT_FLAG { get; set; }

		// Premium Values
		public string? DRIVER_PREMIUM { get; set; }
		public string? ADD_PREMIUM_VALUE { get; set; }
		public string? ADD_PREMIUM_PERCENT { get; set; }
		public string? TOTAL_ALL_PREMIUM { get; set; }

		public string? DEDUCTIBLE_PREMIUMS { get; set; }
		public string? DEDUCTIBLE_PREMIUMS_TAX { get; set; }

		public string? PREMIUM_VALUE { get; set; }
		public string? PREMIUM_OVERALL { get; set; }

		public string? TAX_VALUE { get; set; }
		public string? STAMP_VALUE { get; set; }

		public string? FINE_ADDITIONAL_VALUE { get; set; }

		// Contract Info
		public string? CONTRACT_NUMBER { get; set; }
		public string? PAYMENT_ID { get; set; }
	}
}