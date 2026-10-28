using EskaCMS.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.ModelVM.Request
{
    public class TimeRequest {
        public TimeIntervals? time { get; set; }
        public PeriodSearch? search {  get; set; }

            }
    public class PeriodSearch
    {
      public  DateTime? dateFrom {  get; set; }
      public  DateTime? dateTo { get; set; } 
       public TimeIntervals time {  get; set; }
    }
}
