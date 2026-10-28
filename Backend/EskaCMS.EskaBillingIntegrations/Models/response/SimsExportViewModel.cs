using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.EskaBilling.Models.response
{
    public class SimsExportViewModel
    {
        public long AccountId { get; set; }
        public string ProductName { get; set; }
        public long? ICCID { get; set; }
        public long? MSISDN { get; set; }
        public long? IMSI { get; set; }
        public string SIMName { get; set; }
        public string SIMTag { get; set; }
        public string AssignIP { get; set; }
        public string APNType { get; set; }
        public string City { get; set; }
        public string Status { get; set; }
    }
}
