using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.General.ViewModels
{
    public class LookupSearchVM
    {
        public string SearchValue { get; set; }
        public long SiteId { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
    public class LookupListOutputVM
    {
        public lookupViewVM List { get; set; }
        public int TotalCount { get; set; }

    }
}
