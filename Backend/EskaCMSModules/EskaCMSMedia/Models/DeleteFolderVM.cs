using EskaCMS.Media.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Media.Models
{
    public class DeleteFolderVM
    {
        public List<long> FolderIds { get; set; }
        public MediaActionsEnum Action { get; set; }
    }
}
