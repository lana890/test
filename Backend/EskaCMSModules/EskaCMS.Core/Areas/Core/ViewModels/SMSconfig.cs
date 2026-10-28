using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class SMSconfig
    {
        public string service_type { get; set; }
        public string recipient_numbers_type { get; set; }
        public string content { get; set; }
        public string sender_id { get; set; }
        public string ContentType { get; set; }

        public string URL { get; set; }
        public string APIKey { get; set; }
        public string SenderId { get; set; }
    }

    public class SMSZainconfig
    {
        public string sender_id { get; set; }
        public string ContentType { get; set; }
        public string URL { get; set; }
        public string IntegrationTokenURL { get; set; }
        public string IntegrationToken { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }
    public class SMSJorMallconfig
    {
        public string service_type { get; set; }
        public string recipient_numbers_type { get; set; }
        public string content { get; set; }
        public string sender_id { get; set; }
        public string ContentType { get; set; }

        public string URL { get; set; }
        public string APIKey { get; set; }
        public string SenderId { get; set; }
        public string AccName { get; set; }
        public string AccPass { get; set; }
        public long requesttimeout { get; set; }
    }

    public class SMSMPPconfig
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Source_Number { get; set; }
        public string Type { get; set; }
        public string URL { get; set; } 
    }
}
