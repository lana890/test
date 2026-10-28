using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.NotificationManager.Models
{
    public class NewsLetterSearchParamsDto
    {
        public int PageNumber { get; set; } = 0;
        public int PageSize { get; set; }
        public string? SearchText { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.CoreNotificationType? NotificationType { get; set; }
        public NewsLetterSearchParamsDto()
        {
            this.PageNumber = 0;
            this.PageSize = 20;
            this.SearchText = null;
     //       this.NotificationType = null;

        }
    }
}
