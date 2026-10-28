using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.UsersAndRolesManagement
{
    public class UserQueries : EntityBase
    {
        public string Api { get; set; }
        public string Name { get; set; }
        public string Payload { get; set; }
        public string PageName { get; set; }
        public EPlatformSource Channel { get; set; }
        public long? UserId { get; set; }
        public User User { get; set; }
        public long SiteId { get; set; }
        public Site Site { get; set; }
        public DateTimeOffset CreationDate { get; set; }


    }
}
