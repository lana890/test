using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Models
{
    public class SlugCacheVM
    {
        public string Slug { get; set; }
        public long CultureId { get; set; }
    }

    public class CachingCheckerViewModel
    {
        public string Slug { get; set; }

    }
}
