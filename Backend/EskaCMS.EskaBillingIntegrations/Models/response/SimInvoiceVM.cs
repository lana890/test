using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Models.response
{
    public class SimInvoiceVM
    {
        public long Id { get; set; }
        public string InvoiceNumber { get; set; }
        public string SimName { get; set; }
        public string CustomerName { get; set; }
        public string ServiceName { get; set; }
        public long AccountId { get; set; }
        public long ServiceId { get; set; }
        public string SuspendedPeriod { get; set; }
        public string TermenationPeriod { get; set; }
        public string ActivationPeriod { get; set; }
        public string WatingPeriod { get; set; }
        public string DataUsage { get; set; }
        public string RemainingQuata { get; set; }
        public string TotalAmount { get; set; }
        public string SubAmount { get; set; }
        public string TaxAmount { get; set; }
        public string TaxRate { get; set; }

        public string EndDate { get; set; }
        public string StartDate { get; set; }
        public string ActivationDate { get; set; }
        public string CreationDate { get; set; }
        public long? ICCID { get; set; }
        public long? MSISDN { get; set; }
        public long? IMSI { get; set; }
        public int Quantity { get; set; }
        public string CompanyVatId { get; set; }
        public string CustomerVatId { get; set; }
        public string CustomerAddress { get; set; }
        public string Description { get; set; }
        public string PreviousAmount { get; set; }
        public string APNType { get; set; }
        public string PaymentStatus { get; set; }
    }

    public class SummarySimInvoiceVM
    {
        public long ServiceId { get; set; }
        public long AccountId { get; set; }

       public string TotalAmount { get; set; }
       public string TotalInvoicesAmount { get; set; }

        public int Quantity { get; set; }

        public string Description { get; set; }
        public string SubAmount { get; set; }
        public string TaxAmount { get; set; }
        public string TaxRate { get; set; }
        public string EndDate { get; set; }
        public string StartDate { get; set; }
        public string CreationDate { get; set; }
        public string CustomerName { get; set; }
        public string ServiceName { get; set; }
     
        public string CompanyVatId { get; set; }
        public string CustomerVatId { get; set; }
        public string CustomerAddress { get; set; }

        
    }
}
