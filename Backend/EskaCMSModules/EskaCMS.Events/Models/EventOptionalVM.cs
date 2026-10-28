using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Events.Models
{
    public class EventOptionalVM
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
        public TimeSpan? FromTime { get; set; }
        public TimeSpan? ToTime { get; set; }
        public long? LocationId { get; set; }

        public long? CategoryId { get; set; }
        public long? SiteId { get; set; }
        public long? CultureId { get; set; }
    }
}
