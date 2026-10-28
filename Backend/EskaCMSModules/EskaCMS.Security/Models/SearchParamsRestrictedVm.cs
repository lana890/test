using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models
{
    public class SearchParamsRestrictedVm
    {

        public string SearchValue { get; set; }

        public int PageSize { get; set; }

        public int PageNumber { get; set; }
    }
}
