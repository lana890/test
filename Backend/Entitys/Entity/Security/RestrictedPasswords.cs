using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Security
{
    public class RestrictedPasswords:EntityBase
    {
        //public SitesSecurityPolicy SecurityPolicy { get; set; }
        //public long SecurityPolicyId { get; set; }


        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }


        public string RestrictedText { get; set; }

        public virtual User CreatedBy { get; set; }
        public long? CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }

        public DateTimeOffset ModificationDate { get; set; }
        public User ModifiedBy { get; set; }
        public long? ModifiedById { get; set; }

    }
}
