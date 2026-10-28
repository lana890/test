using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.UserDashboard
{
    public class UserVisits : EntityBase
    {


        public DateTimeOffset CreationDate { get; set; }


        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }


        public long UserId { get; set; }
        public virtual User User { get; set; }

        public IList<UserNavigations> UserNavigations { get; set; }
    }
}
