using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Models
{
    public class ParamsViewModel
    {
        public int? PageSize { get; set; } = null;
        public int? PageNumber { get; set; } = null;
        public string SearchText { get; set; }
    }
     
}
