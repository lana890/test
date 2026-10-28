using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Media.Models
{
    public class TransferFolderVm
    {
        public List<long> CurrentFolderIds { get; set; }
        public long? TargetFolderId { get; set; }
    }
}
