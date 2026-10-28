using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.EskaBilling.Models
{
    public class GetSIMUsageFilterVM
    {
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }
        public long SIMId { get; set; }
    }

    public class GetSIMUsageResponseVM
    {

        public string SimName { get; set; }
        public double DataTotal { get; set; }
        public double TotalQuota { get; set; }
        public string Converted_DataTotal { get; set; }
        public string Converted_TotalQuota { get; set; }
        public double RoamingDataConsumed { get; set; }
        public string Converted_RoamingDataConsumed { get; set; }
        public double DataOut { get; set; }
        public double Cost { get; set; }
        public long SMS_Outgoing { get; set; }
        public double DataCharged { get; set; }
        public double SMS_Usage { get; set; }
        public double RoamingDataOut { get; set; }
        public double RoamingVoiceOutgoing { get; set; }
        public long NumberOfVoiceCalls { get; set; }
        public long NumberOfRoamingSms { get; set; }
        public long RoamingSmsOutgoing { get; set; }
        public double VoiceOutgoing { get; set; }
        public double VoiceIncoming { get; set; }
        public long NumberOfRoamingCalls { get; set; }
        public double RoamingVoiceIncoming { get; set; }
        public double DataIn { get; set; }
        public long SMS_Incoming { get; set; }
        public long RoamingSmsIncoming { get; set; }
        public double RoamingDataIn { get; set; }


        public EIOTSIMStatus StatusSnapshot { get; set; }
        public DateTimeOffset CreationDate { get; set; }

        public long CustomerSimsId { get; set; }
    }


    public class GetBillingUsageParameter
    {
        public GetBillingUsageParameter(string Identifier, string SimId)
        {
            if (!SIMsIdentifiers.IsValidIdentifier(Identifier))
            {
                throw new Exception("Cannot create ChangeSimPlanParameter object due Invalid Identifier.");
            }
            this.Identifier = Identifier;
            this.SimId = SimId;

        }

        public string SimId { get; set; }
        public string Identifier { get; set; }

    }
    public class UpdateSTC_IP
    {
        public UpdateSTC_IP(string Identifier, string SimId, string IPAdress)
        {
            if (!SIMsIdentifiers.IsValidIdentifier(Identifier))
            {
                throw new Exception("Cannot create ChangeSimPlanParameter object due Invalid Identifier.");
            }
            this.Identifier = Identifier;
            this.SimId = SimId;
            this.IPAdress = IPAdress;

        }

        public string SimId { get; set; }
        public string Identifier { get; set; }
        public string IPAdress { get; set; }

    }
    public class SIMWithIdentifiers
    {
        public long Id { get; set; }
        public long? IMSI { get; set; }
        public long? ICCID { get; set; }
        public ESIMsIdentifiers IdentifierType { get; set; }
    }


    public class GetAllSimUsageResponseVM
    {

        
        public DateTimeOffset CreationDate { get; set; }
        public  List<GetSIMUsageResponseVM> SimsUsage { get; set; }

    }

    public class GetAllSIMUsageFilterVM
    {
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }
        public long AccountId { get; set; }
        public string SimTag { get; set; }
    }
}
