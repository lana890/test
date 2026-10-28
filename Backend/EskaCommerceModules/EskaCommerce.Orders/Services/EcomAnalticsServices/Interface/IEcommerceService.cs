using EskaCMS.Entities.Entity.Orders.Models;
using EskaCMS.Entities.Enum;
using EskaCMS.UserDashboard.ViewModels;
using EskaCommerce.Orders.Enum;
using EskaCommerce.Orders.ModelVM;
using EskaCommerce.Orders.ModelVM.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Services.EcomAnalticsServices.Interface
{
    public interface IEcommerceService
    {
        Task<OrderChartVM> Income(TimeIntervals? time = null, PeriodSearch? search = null);
        Task<OrderChartVM> ConversionRate(TimeIntervals? time = null, PeriodSearch? search = null);
        Task<OrderChartVM> TotalOrders(TimeIntervals? time = null, PeriodSearch? search = null);
        Task<OrderChartVM> AbondomentRate(TimeIntervals? time = null, PeriodSearch? search = null);
        Task<List<MostSearchedVM>> MostSearchedProducts(TimeIntervals? time = null, PeriodSearch? search = null);
        Task<DashboardDto> CustomerAnalyticsDashboard(TimeIntervals? time = null, PeriodSearch? search = null);
        Task<List<CartOrderChart>> GetCartOrderCharts(TimeIntervals? time = null, PeriodSearch? search = null);
        Task<List<TrafficSourceVM>> AbandonmentByTrafficSource(TimeIntervals? time = null, PeriodSearch? search = null);
        Task<List<TrafficDeviceVM>> AbandonmentByDevice(TimeIntervals? time = null, PeriodSearch? search = null);
        Task<List<SalesFunnalVM>> SalesFunnel(filterType filter, TimeIntervals? time = null, PeriodSearch? search = null);
        Task<List<SourceClicks>> ClickThroughRate(TimeIntervals? time = null, PeriodSearch? search = null);
        Task<List<SourceClicks>> ProductTrafficSource(TimeIntervals? time = null, PeriodSearch? search = null);
        Task<List<ProductViewsVM>> Product_Views(int? productId, TimeIntervals? time = null, PeriodSearch? search = null);
        Task<ProductCardsVM> product_insights(TimeIntervals? time = null, PeriodSearch? search = null);



    }
}
