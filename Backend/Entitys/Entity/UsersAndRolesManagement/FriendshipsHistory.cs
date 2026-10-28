using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.FriendshipEnums;

namespace EskaCMS.Entities.Entity.UsersAndRolesManagement
{
    public class FriendshipsHistory : EntityBase
    {
        public long FriendshipId { get; set; }
        public Friendships Friendship { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public EFriendshipStatus Status { get; set; }
    }
}
