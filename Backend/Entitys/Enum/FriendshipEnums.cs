using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Enum
{
    public class FriendshipEnums
    {
        public enum EFriendshipStatus
        {
            Requested = 1,
            Accepted = 2,
            Rejected = 3,
            Deleted = 4,

        }

        public enum EUserProfileStatus
        {
           NotFriend=1,
           Friend=2,
           SentRequest=3,
            ReceivedRequest = 4,

        }

    }
}
