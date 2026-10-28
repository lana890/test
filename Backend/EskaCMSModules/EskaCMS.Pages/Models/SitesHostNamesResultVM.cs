using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Pages.Models
{
    public class SitesHostNamesResultVM
    {
        public long CultureId { get; set; }
        public string CultureName { get; set; }
        public string Prefix { get; set; }
        public string Hostname { get; set; }
    }
}
