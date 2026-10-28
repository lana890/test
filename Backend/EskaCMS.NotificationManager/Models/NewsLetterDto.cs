using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.NotificationManager.Models
{
    public class NewsLetterDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public bool IsMuted { get; set; }
    }
}
