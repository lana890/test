using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Core.Models
{
 public   class SiteSettingsVM
    {
        public string Id { get; set; }
     
        public string Value { get; set; }
     
        public string Module { get; set; }

        public bool IsVisibleInCommonSettingPage { get; set; }
        public long SiteId { get; set; }
    }
}
