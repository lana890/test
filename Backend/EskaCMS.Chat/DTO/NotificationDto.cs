using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Chat.DTO
{
    public class NotificationDto
    {
        public long RecipientUserId { get; set; }
        public string EmailTemplateId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public long SiteId { get; set; }
    }

    public class NotificationUpdateDto
    {
        public long NotificationSchemeId { get; set; }
        public long UserId { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
