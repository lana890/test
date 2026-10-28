using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Core.Models
{
  public  class UniFonicSMSVM
    {
        public string ApiURL { get; set; }
        public string Sender { get; set; }
        public string AppsId { get; set; }
        public string Encoding { get; set; }
    }


    public class MashastraSMSVM : UniFonicSMSVM
    {
        public string SMSType { get; set; }
        public string CountryCode { get; set; }
        public string SMSUserId { get; set; }
        public string pwd { get; set; }


    }
}
