using EskaCMS.Core.Extentions.Settings;
using EskaCMS.Core.Models;
using System.Collections.Generic;

namespace EskaCommerce.Core.Manage
{
    public class UserSettingViewModel
    {
        public UserSettingViewModel()
        {
            UserSettings = new List<SiteSettingsVM>();
            SettingDefinitions = new Dictionary<string, SettingDefinition>();
        }

        public Dictionary<string, SettingDefinition> SettingDefinitions { get; set; }
        public List<SiteSettingsVM> UserSettings { get; set; }
    }
}
