using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Core.Models
{
    public class SMSGlobalSettings
    {
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string method { get; set; }
        public string host { get; set; }
        public string port { get; set; }
        public string version { get; set; }
        public string path { get; set; }
        public string smsid { get; set; }
        public string origin { get; set; }
    }
}
