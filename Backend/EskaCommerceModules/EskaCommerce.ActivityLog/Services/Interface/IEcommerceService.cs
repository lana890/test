using EskaCMS.Entities.Enum;
using EskaCMS.UserDashboard.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.UserDashboard.Services.Interfaces
{
    public interface IEcommerceService
    {
        Task<IncomeResponse> Income(TimeIntervals time);
      Task<IncomeResponse> ConversionRate(TimeIntervals time);
       Task<IncomeResponse> TotalOrders(TimeIntervals time);
       Task<IncomeResponse> AbondomentRate(TimeIntervals time);
    }
}
