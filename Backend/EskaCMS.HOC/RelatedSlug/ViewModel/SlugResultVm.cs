using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.HOC.RelatedSlug.ViewModel
{
    public class SlugResultVm
    {
        public long CultureId { get; set; }
        public string CultureName { get; set; }
        public string Name { get; set; }

        public string Slug { get; set; }
        public string HostName { get; set; }
        public string Prefix { get; set; }

    }

    public class SlugsInfo
    {
        public string Name { get; set; }
        public string Slug { get; set; }

    }
}
