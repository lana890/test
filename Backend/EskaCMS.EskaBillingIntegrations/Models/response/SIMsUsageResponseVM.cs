using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Models.response
{
    public class SIMsUsageResponseVM
    {
        public double dataTotal { get; set; }
        public double roamingDataConsumed { get; set; }
        public double dataOut { get; set; }
        public double cost { get; set; }
        public long smsOutgoing { get; set; }
        public double dataCharged { get; set; }
        public double smsUsage { get; set; }
        public double roamingDataOut { get; set; }
        public double roamingVoiceOutgoing { get; set; }
        public long numberOfVoiceCalls { get; set; }
        public long numberOfRoamingSms { get; set; }
        public long roamingSmsOutgoing { get; set; }
        public double voiceOutgoing { get; set; }
        public double voiceIncoming { get; set; }
        public long numberOfRoamingCalls { get; set; }
        public double roamingVoiceIncoming { get; set; }
        public double dataIn { get; set; }
        public long smsIncoming { get; set; }
        public long roamingSmsIncoming { get; set; }
        public double roamingDataIn { get; set; }

    }
}
