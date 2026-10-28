using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Models.response
{
    public class InvoiceDetailsVM
    {
        public List<Period> ActivePeriods { get; set; }
        public List<Period> TerminatedPeriods { get; set; }
        public List<Period> SuspendedPeriods { get; set; }
        public List<Period> WatingPeriods { get; set; }
    }

    public class Period
    {
       
        public DateTimeOffset From { get; set; }

       
        public DateTimeOffset To { get; set; }
    }
}
