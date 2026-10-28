using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.NotificationManager.Models
{
    public class UsersGroupDto
    {
       public long GroupId { get; set; }
       public string GroupName { get; set; }
       public IList<UsersNewsLetter> Users { get; set; }

    }
    public class UsersEmailsDto
    {
        public string Email { get; set; }
    }
    public class UsersNewsLetter
    {
        public long Id { get; set; }
        public string UserName { get; set; }

        public string Email { get; set; }

    }

    public class SubscribedUsers
    {
        public long Id { get; set; }
        public bool IsMuted { get; set; }
        public string UserName { get; set; }

        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string DeviceToken { get; set; }

    }
}
