using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Chat
{
    public class Message: EntityBase
    {
        public long SenderId { get; set; }

        public User Sender { get; set; }

        public long? RecipientId { get; set; }

        public User Recipient { get; set; }


        public long? RecipientCustomerGroupId { get; set; }

        public CustomerGroup RecipientCustomerGroup { get; set; }
        public List<MassagesScheduledGroup>? MassagesScheduledGroup { get; set; }

        public string Content { get; set; }
        public DateTimeOffset? DateRead { get; set; }

        public DateTimeOffset MessageSent { get; set; } = DateTimeOffset.UtcNow;

        public bool SenderDeleted { get; set; }

        public bool RecipientDeleted { get; set; }
        public bool IsViolation { get; set; }
        public long? SiteId { get; set; }
        public Site Site { get; set; }
      //[NotMapped]
      //  public long? ReplyId { get; set; }
      // [NotMapped]

      //  public Message Reply { get; set; }
      //  [NotMapped]

     //   public bool IsForwarded { get; set; } = false;


        public long? ParentId { get; set; }

        public Message Parent { get; set; }
    }
}
