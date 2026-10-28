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
    public class CustomerInvoices:EntityBase
    {
        public long AccountId { get; set; }
        public long ServiceId { get; set; }

        public EInvoiceStatus PaymentStatus { get; set; }
        public TimeSpan SuspendedPeriod { get; set; }
        public TimeSpan TermenationPeriod { get; set; }
        public TimeSpan ActivationPeriod { get; set; }
        public TimeSpan WatingPeriod { get; set; }
        public double DataUsage { get; set; }
        public double RemainingQuata { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SubAmount { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? TaxAmount { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset ActivationDate { get; set; }
        public long? ICCID { get; set; }
        public long? MSISDN { get; set; }
        public long? IMSI { get; set; }
        public string APNType { get; set; }
        public long CustomerSimsId { get; set; }
        public virtual CustomerSims CustomerSims { get; set; }

        public long CustomerId { get; set; }
        public virtual User Customer { get; set; }
        public long ProductId { get; set; }
        public virtual Product Product { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public long? ModifiedById { get; set; }
        public virtual User ModifiedBy { get; set; }
    }
}
