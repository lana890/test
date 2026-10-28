using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Pages.Models
{
    public class FilterComponentsSettingsVM
    {
        public string ComponentName { get; set; }
        public IList<long> PageIds { get; set; } = new List<long>();
        public IList<long> CultureIds { get; set; } = new List<long>();
    }
}
