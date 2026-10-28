using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.EskaBilling.Models
{
    public class ChangeInvoiceStatusVM
    {
        public List<long> InovicesId { get; set; }
        public EInvoiceStatus Status { get; set; }
    }
}
