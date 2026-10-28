using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Media.Models
{
    public class RestoreMediaVM
    {
        public List<long> MediaIds { get; set; }
        public List<long> FolderIds { get; set; }
    }
}
