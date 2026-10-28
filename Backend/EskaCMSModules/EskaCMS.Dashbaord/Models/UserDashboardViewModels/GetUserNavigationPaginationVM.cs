using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.UserDashboard.ViewModels
{
    public class GetUserNavigationPaginationVM
    {
        public List<UsersByVisitResultVM> usersByVisitResultVMs { get; set; }
        public int TotalUsersVisites { get; set; }
    }
}
