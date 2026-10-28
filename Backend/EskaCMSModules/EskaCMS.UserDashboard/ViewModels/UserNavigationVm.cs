using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.UserDashboard.ViewModels
{
    public class UserNavigationVm
    {
        public string Slug { get; set; }
        public string BrowserName { get; set; }
        public string BrowserVersion { get; set; }

    }


    public class NavigateToSlug
    {
        public string Slug { get; set; }
    }
}
