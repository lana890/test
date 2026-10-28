using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.EmailSender.Services.Configuration
{
    public class EmailConfig
    {
        public string SmtpServer { get; set; }

        public int SmtpPort { get; set; }

        public string SmtpUsername { get; set; }

        public string SmtpPassword { get; set; }
    }
}
