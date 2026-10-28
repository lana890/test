using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Core
{
    public class EmailsSettings:EntityBase
    {
        public string Name { get; set; }
        public string SmtpUsername { get; set; }
        public string? SmtpPassword { get; set; } = null;
        public string SmtpSenderEmailAdmin { get; set; } = null;
        public string SmtpServer { get; set; }
        public string SmtpSenderEmail { get; set; }
        public int SmtpPort { get; set; }
        public bool RequiresAuthentication { get; set; }
        public bool IsDefault { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus  Status { get; set; }
        public long SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }

    }
}
