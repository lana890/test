using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models.FriendshipModels
{
    public class UserRequestsDto
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Image { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        
    }
    
    public class UserFriendsDto
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Image { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? AcceptDate { get; set; }
        public DateTimeOffset? StatusDate { get; set; }

    }
}
