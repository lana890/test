using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.ViewModels.LoggedInUser
{
    public class LoggedInUserPaginationVM
    {
        public List<LoggedInUserVM> loggedInUsers { get; set; }


        public int TotalLoggedInUsers { get; set; }
    }
}
