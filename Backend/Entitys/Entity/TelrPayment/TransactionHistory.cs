using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Entity.TelrEnums;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Payments;

namespace EskaCMS.Entities.Entity.TelrPayment
{
    public class TransactionHistory : EntityBase
    {

        public Payment Payment { get; set; }
        public long? PaymentId { get; set; }
        public Order Order { get; set; }
        public long OrderId { get; set; }
        public long  TelrStoreId { get; set; }
        public string TransType { get; set; }
        public TelrTransTest TransTest { get; set; }
        public string TransClass { get; set; }
        public long TransRefId { get; set; }
        public long TransPrevRefId { get; set; }
        public long TransFirstRefId { get; set; }


        public string TransCartId { get; set; }
        
        public string TransStatus { get; set; }
        public string TransAuthCode { get; set; }
        public string TransAuthMessage { get; set; }
        public string TransCheck { get; set; }

        public string CardCode { get; set; }
        public string CardLast4 { get; set; }
        public string CardCheck { get; set; }
        public string CartLang { get; set; }
        public long IntegrationId { get; set; }
        public string AgreementId { get; set; }
        public string Note { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? StatusDate { get; set; }

        public TransactionHistory Parent { get; set; }
        public long? ParentId { get; set; }


    }
}
