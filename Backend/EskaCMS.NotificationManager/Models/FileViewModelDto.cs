using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.NotificationManager.Models
{
    public class FileViewModelDto
    {
        public long Id { get; set; }
        public string fileName { get; set; }
        public string contentType { get; set; }

        public string publicUrl { get; set; }
    }
}
