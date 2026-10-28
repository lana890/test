using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.NewsLetter
{
    public class NewsLetterStatistics : EntityBase
    {
        public long NewsLetterId { get; set; }
        public virtual NewsLetters NewsLetter { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public long SuccessCount { get; set; }

        public long ExpectedRecipientCount { get; set; }

        public long FailedNotificationCount { get; set; }
        public long MutedUserCount { get; set; }

        public List<NewsLetterViwers> NewsLetterViwers { get; set; }
    }
}
