using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.UserDashboard.ViewModels
{
       public class IncomeResponse
       {
        public Dictionary<string,IncomeVM> response { set; get; }
        }

        public class IncomeVM
        {
        public double value {  set; get; }
        public double trend {  set; get; }
        public bool isPositive {  set; get; }
         }

}
