using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Pages.Models
{
    public class GetAllPagesByHostnameVM
    {
        public string Hostname { get; set; }
        public long CultureId { get; set; }
    }
}
