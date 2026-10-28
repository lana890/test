using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Pages.Models
{
    public class GetSpecificSettingsVM
    {
        public string ComponentName { get; set; }
        public string PageName { get; set; }
        public string Slug { get; set; }
        public string KeyName { get; set; }
    }
}
