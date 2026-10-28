

using System.Collections.Generic;

namespace EskaCMS.Infrastructure.Web.SmartTable
{
    public class SmartTableParam
    {
        public Pagination Pagination { get; set; }

        public Search Search { get; set; }

        public Sort Sort { get; set; }
    }
    public class SmartExportTableParam
    {
        public Pagination Pagination { get; set; }

        public Search Search { get; set; }

        public Sort Sort { get; set; }

        public List<string> Properties { get; set; }

    }
}
