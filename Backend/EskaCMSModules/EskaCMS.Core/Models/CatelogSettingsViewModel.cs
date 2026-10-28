using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Models
{
    public class CatelogSettingsViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ThumbnailImageUrl { get; set; }
        public string CategoryName { get; set; }
        public int DisplayOrder { get; set;}
        public long ProductId { get; set; }
    }
}
