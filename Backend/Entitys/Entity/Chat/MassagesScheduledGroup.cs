using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Site = EskaCMS.Entities.Entity.Core.Site;

namespace EskaCMS.Entities.Entity.Chat
{
    public class MassagesScheduledGroup: EntityBase
    {
        public long CustomerGroupId  { get; set; }
        public CustomerGroup  CustomerGroup { get; set; }

        public long MessageId { get; set; }
        public Message Message { get; set; }

        public long SiteId { get; set; }
        public Site Site { get; set; }
        public long UserId { get; set; }
        public User User { get; set; }
        public DateTimeOffset? DateRead { get; set; }
    }
}
