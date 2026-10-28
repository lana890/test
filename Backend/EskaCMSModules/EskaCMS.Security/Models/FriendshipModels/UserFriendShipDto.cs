using EskaCMS.Core.BusinessModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.FriendshipEnums;

namespace EskaCMS.Security.Models.FriendshipModels
{
    public class UserFriendShipDto
    {
        public string UserId { get; set; }
        public long Id { get; set; }

        public string ProfileImage { get; set; }
        public string CoverImage { get; set; }
        public EUserProfileStatus Status { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public int? AvailabilityStatus { get; set; }

        public List<UserOptionsVM> UserOptions { get; set; }
    }

    public class UserFriendDto
    {
        public long UserId { get; set; }

        public string UserName { get; set; }

    }
}
