using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Pages.Models
{
    
    public class SettingPageViewModel
    {
        public PagesViewModel Page { get; set; }
        public IList<VersionsViewModel> Versionslist { get; set; }
        public string SettingsBody { get; set; }
    }

}
