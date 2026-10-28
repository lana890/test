using EskaCMS.EskaBilling.Models.response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.EskaBilling.Models
{
    public class CustomerInvoicesVm
    {
       
        //public List<SimInvoiceVM> SimsInovice { get; set; }

        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public long AccountId { get; set; }
        public string CreationDate { get; set; }
        public string CompanyVatId { get; set; }
        public string CustomerVatId { get; set; }
        public string PreviousUnPaidAmount { get; set; } 
        public string TotalAfterTaxAmount { get; set; } 
        public string TotalSubAmount { get; set; } 
        public string TotalTaxAmount { get; set; } 
        public string TotalDueAmount { get; set; } 

    }

   
}
