using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models.FriendshipModels
{
    public class AllUsersVM
    {
        public List<long>? RoleIds { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }

    }

    public class NotificationDto
    {
        public long RecipientUserId { get; set; }
        public string EmailTemplateId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string SenderFullName { get; set; }
        public string ReciverFullName { get; set; }
        public long SiteId { get; set; }

    }
}
