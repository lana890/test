using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Addresses.Model
{
    public class ShippableStatesVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class SearchShippableStatesVM
    {
        public string countryId { get; set; }
        public long? stateId { get; set; }
        public string searchtext { get; set; }
        public int? pagesize { get; set; }
        public int? pagenumber { get; set; }
    }
}
