using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Entities.Entity.Payments
{
    public class PaymentRefund : EntityBase
    {

        public Payment Payment { get; set; }
        public long PaymentId { get; set; }

        public RefundPaymentStatus RefundStatus { get; set; }

        public string Tranref { get; set; }

        public string AdditionalInfo { get; set; }



    }
}
