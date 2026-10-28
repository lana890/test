using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.ViewModels.ComponentKeys
{
    public class ComponentKeysVM
    {
        public long Id { get; set; }
        public string Key { get; set; }
        public long? PageId { get; set; }
        public long? TemplatePageId { get; set; }

        public long SiteId { get; set; }

        public bool IsConFirmed  { get; set; }

    }

    public class ComponentKeysUpdate
    {
        public List<string> Key { get; set; }=new List<string>();

    }
}