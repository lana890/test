using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.NotificationManager.Models
{
    public class NewsLetterViewersVm
    {
        public long Id { get; set; }

        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public DateTimeOffset ViewDate { get; set; }
    }
}
