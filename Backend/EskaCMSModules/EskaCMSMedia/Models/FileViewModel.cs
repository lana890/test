using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Media.Model
{
   
    public class FileViewModel
    {
        public string fileName { get; set; }
        public string contentType { get; set; }

        public string publicUrl { get; set; }
        public int size { get; set; }
        public DateTimeOffset CreationDate { get; set; }


    }
}
