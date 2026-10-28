using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity
{
    public class ImportedFilesVM
    {
        public long  Id { get; set; }
        public long SourceId { get; set; } // template or theme Id
        public long FileId { get; set; }
        public string FileUrl { get; set; }
        public string contentType { get; set; }

        public string  FileName { get; set; }


    }
}
