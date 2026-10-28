using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.TelrPayment.Models
{
  public  class TelrServicesConfig
    {

        public string TransactionSearchCartId { get; set; }

        public string TransactionSearchEmail { get; set; }

        public string TransactionDetails { get; set; }

        public string AgreementList { get; set; }

        public string AgreementDetails { get; set; }
        public string AgreementHistory { get; set; }
        public string CancelAgreement { get; set; }
    }
}
