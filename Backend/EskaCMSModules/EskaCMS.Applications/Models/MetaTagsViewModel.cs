using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Sites.Services.SitesService;

namespace EskaCMS.Sites.Models
{
    public class MetaTagsViewModel
    {
        public List<List<MetaTagItem>> MetaTags { get; set; }
        public JArray PrimaryTags { get; set; }


    }
    public class MetaTagItem
    {
        public string key { get; set; }
        public string value { get; set; }
    }
    public class keyvalueViewModel
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
