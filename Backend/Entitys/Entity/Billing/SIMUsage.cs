using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.EskaBilling.Enums.IOTEnums;


namespace EskaCMS.Entities.Entity
{
    public class SIMUsage : EntityBase
    {

        public double DataTotal { get; set; }
        public double RoamingDataConsumed { get; set; }
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

        public long CustomerSimsId { get; set; }
        public virtual CustomerSims CustomerSims { get; set; }


        public long ProductId { get; set; }

        public Product Product { get; set; }


        public long? CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
}
