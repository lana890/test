using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.FriendshipEnums;

namespace EskaCMS.Entities.Entity.UsersAndRolesManagement
{
    public class Friendships : EntityBase
    {

        public DateTimeOffset? AcceptedDate { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? StatusDate { get; set; }
        public EFriendshipStatus Status { get; set; }
        public long FromId { get; set; }
        public User From { get; set; }
        public long ToId { get; set; }
        public User To { get; set; }
        public long SiteId { get; set; }
        public Site Site { get; set; }
    }
}
