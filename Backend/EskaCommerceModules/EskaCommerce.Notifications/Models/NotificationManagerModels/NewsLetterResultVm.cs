using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.NotificationManager.Models
{
    public class NewsLetterResultVm
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public CoreNotificationType NotificationType { get; set; }

        public DateTimeOffset? SendDate { get; set; }
        public int SendTo { get; set; }
        public Entities.Enum.NewsLetterType TypeOfSend { get; set; }
        public Entities.Enum.ESendStatus? SendStatus { get; set; }
        public int SubscribedGroups { get; set; }

    }

    public class NewsLetterRecordesDto
    {
        public List<NewsLetterResultVm> NewsLetters { get; set; }
        public int TotalRecords { get; set; }
    }
}
