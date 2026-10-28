using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace EskaCMS.Integrations.Models.CBVehicleAssessment
{


    public class VehicleAssessmentRequest
    {
        public Report report { get; set; } = new();
    }

    public class Report
    {
        public CasdameReport CaseDamageReport { get; set; } = new();
    }

    public class CasdameReport
    {
        public string OverallStatus { get; set; } = string.Empty;
    }


    public class ApprovalRequest
    {
        public string ConditionPercentage { get; set; } = string.Empty;

		public string AssesmentType { get; set; } 
		public string OverAllDamagePercent { get; set; } 


	}

	public class ApprovalResponse
    {
        public string ResultName { get; set; } = string.Empty; // "APPROVE" or "REJECT"
    }


    public class JoclaimIntegRequest
    {
        public string Vin { get; set; } = string.Empty;
        public string PlateNo { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerPhone { get; set; } = string.Empty;
    }

    public class JoclaimTokenResponse
    {
         
        public string access_token { get; set; } = string.Empty;

         public string token_type { get; set; } = string.Empty;

         public string expires_in { get; set; } = string.Empty;
    }


    public class SaveAICustomerResponse
    {
        public string AICaseID { get; set; } = string.Empty;
        public string AIToken { get; set; } = string.Empty;
        public string InspectionLink { get; set; } = string.Empty;
    }
    public class GetVehicleHistoryVM
    {
       public string vin { get; set; }=string.Empty;
       public string language {  get; set; } = string.Empty;
    }


}
