using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class GetEmailResponseVm
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public string SmtpUsername { get; set; }


        public string SmtpServer { get; set; }
       
        public string SmtpSenderEmail { get; set; }
        public string SmtpSenderEmailAdmin { get; set; }

        public int SmtpPort { get; set; }

        public bool RequiresAuthentication { get; set; }

        public bool IsDefault { get; set; }
    }
}
