using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.EskaBilling.Models
{
    public class SimUsageVM
    {
        public long CustomerId { get; set; }
        public long CustomerSimId { get; set; }
        public long ProductId { get; set; }
        public long ServiceId { get; set; }
        public long AccountId { get; set; }
        public double TotalData { get; set; }

        public double TotalQuota { get; set; }
        public decimal Price { get; set; }

        public decimal? TaxRate { get; set; }
        public EIOTSIMStatus StatusSnapshot { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public long? ICCID { get; set; }
        public long? MSISDN { get; set; }
        public long? IMSI { get; set; }
        public string APNType { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset ActivationDate { get; set; }
        

    }


}
