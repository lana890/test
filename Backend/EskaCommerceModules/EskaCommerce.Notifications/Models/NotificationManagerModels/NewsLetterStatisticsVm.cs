using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.NotificationManager.Models
{
    public class NewsLetterStatisticsVm
    {

        public long StatisticId { get; set; }
        public string NotificationName { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public long ViewerCount { get; set; }

        public long ExpectedRecipientCount { get; set; }

        public long FailedNotificationCount { get; set; }

        public long SuccessNotificationCount { get; set; }
        public long MutedNotificationCount { get; set; }
    }
}
