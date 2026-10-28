using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Chat.DTO
{
    public class CreateMassageDTO
    {
        public long? RecipientUserId { get; set; }
        public long? RecipientCustomerGroupId { get; set; }
        public string Content { get; set; }

        public long? ReplyId { get; set; }
        public bool IsForwarded { get; set; }

        public DateTimeOffset? DateRead { get; set; }
        public long? UserId { get; set; }
        public long? SiteId { get; set; }
    }
    public class UserReadMassagsDTO
    {
        public long? RecipientUserId { get; set; }
        public long? RecipientCustomerGroupId { get; set; }
        public long SiteId { get; set; }
      
    }
}
