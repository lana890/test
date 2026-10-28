using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Media.Models
{

    public class FolderViewModel
    {
        public string Name { get; set; }

        public DateTime? ModificationDate { get; set; }
        public DateTime CreationDate { get; set; }
        public string FolderPath { get; set; }
        public string ParentFolderPath { get; set; }

        public string thumbnailImage { get; set; }

    }
}
