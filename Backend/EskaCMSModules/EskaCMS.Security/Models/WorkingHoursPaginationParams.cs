using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models
{
    public  class PaginationFiltersVM
    {
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public long? customerGroupId { get; set; }
            public string SearchText { get; set; }
        
    }



}
