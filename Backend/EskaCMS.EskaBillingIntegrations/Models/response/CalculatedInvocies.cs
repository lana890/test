using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Models.response
{
    public class CalculatedInvocies
    {
        public decimal TotalSubAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal TotalAfterTax { get; set; }
        public decimal AllPreviousUnpaid { get; set; }
        public decimal AllDueAmount { get; set; }

   
    }
}
