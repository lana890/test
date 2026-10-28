using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Models.response
{
    public class RequestChangePlanVM
    {
        public long Id { get; set; }

        public string Note { get; set; }

        public string SimName { get; set; }
        public string Status { get; set; }
        public string NextProfile { get; set; }
        public string PreviousProfile { get; set; }

        public string RequestedChangeDate { get; set; }
        public string NextMonthActivationDate { get; set; }
    }
}
