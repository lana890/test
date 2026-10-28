using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Media.Models
{
    public class UserFileVM
    {
        public string FileName { get; set; }

        public string PublicUrl { get; set; }
        public string ContentType { get; set; }

        public long Id { get; set; }

    }
}
