using Elastic.Clients.Elasticsearch;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.NewsLetter;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Orders.Models;
using EskaCMS.Entities.Entity.Search;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Entity.UserDashboard;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Search.Areas.Search.ViewModels;
using EskaCMS.UserDashboard.Services.Interfaces;
using EskaCMS.UserDashboard.ViewModels;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Orders.Data;
using EskaCommerce.Orders.Enum;
using EskaCommerce.Orders.ModelVM;
using EskaCommerce.Orders.ModelVM.Request;
using EskaCommerce.Orders.Services.EcomAnalticsServices.Interface;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OfficeOpenXml.Drawing.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.EventEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;
using SearchEntity = EskaCMS.Entities.Entity.Search;
using  EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using Microsoft.AspNetCore.Razor.TagHelpers;
using EskaCMS.Entities.Entity.ProductRecentlyViewed;
namespace EskaCommerce.Orders.Services.EcomAnalticsServices

{
    public class EcommerceService : IEcommerceService
    {
        private readonly IRepository<Order> _orderrepository;
        private readonly IRepository<User> _usersrepository;
        private readonly IRepository<Product> _productrepository;
        private readonly IRepository<UserVisits> _uservisistsRepository;
        private readonly IRepository<SearchEntity.Query> _queryRepository;
        private readonly IWorkContext _workContext;
        private readonly IRepositoryWithCustomId<CustomerGroupUser> _customerGroupUserRepository;
        private readonly IRepository<Analytics_TrafficSource> _trafficRepository;
       private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;
        private readonly IRepository<NewsLetters> _newsletterRepository;
        private readonly IRepository<NewsLetterStatistics> _newsstatiscsRepository;
        private readonly IRepository<NewsLetterViwers> _newsViewsRepository;
        private readonly IRepository<RecentlyViewedProduct> _recentlyViewdProduct;

        public IRepository<Cart> _cartRepository { get; set; }
        public EcommerceService(
            IRepository<Order> orderRepository,
            IRepository<User> usersrepository,
            IRepository<Cart> cartRepository,
            IRepository<Product> productrepository,
            IRepository<SearchEntity.Query> queryRepository,
            IRepositoryWithCustomId<CustomerGroupUser> customerGroupUserRepository,
            IRepository<Analytics_TrafficSource> trafficRepository,
            IRepository<UserVisits> uservisistsRepository,
            IRepositoryWithTypedId<SiteSettings, string> appSettingRepository,
            IRepository<NewsLetters> newsletterRepository,
            IRepository<NewsLetterStatistics> newsstatiscsRepository,
            IRepository<NewsLetterViwers> newsViewsRepository,
             IRepository<RecentlyViewedProduct> recentlyViewdProduct,

        IWorkContext workContext)
        {
            _orderrepository = orderRepository;
            _usersrepository = usersrepository;
            _cartRepository = cartRepository;
            _productrepository = productrepository;
            _queryRepository = queryRepository;
            _workContext = workContext;
            _customerGroupUserRepository = customerGroupUserRepository;
            _trafficRepository = trafficRepository;
            _uservisistsRepository = uservisistsRepository;
            _appSettingRepository =  appSettingRepository;
            _newsletterRepository = newsletterRepository;
            _newsstatiscsRepository = newsstatiscsRepository;
            _newsViewsRepository = newsViewsRepository;
            _recentlyViewdProduct = recentlyViewdProduct;
        }
        //public async Task<OrderChartVM> Income(TimeIntervals? time,PeriodSearch? search)
        //{
        //    try
        //    {
        //        double trend = 0;
        //        if(time.HasValue) {
        //        var (currentStart, currentEnd) = GetDateRange(time, DateTime.UtcNow);
        //        var (previosStart, previosEnd) = GetDateRange(time, DateTime.UtcNow, 1);




        //        var CurrentIncome = await GetIncomeAsync(currentStart, currentEnd);

        //        var PrevIncome = await GetIncomeAsync(previosStart, previosEnd);
        //        if (PrevIncome == 0)
        //        {
        //            trend = 0;

        //        }
        //        else
        //            trend = (double)(CurrentIncome - PrevIncome) / (double)PrevIncome * 100;

        //        return new OrderChartVM
        //        {
        //            response = new Dictionary<string, OrderChartPVM>
        //            {
        //                ["Income"] = new OrderChartPVM
        //                {
        //                    trend = trend,
        //                    value = (double)CurrentIncome,
        //                    isPositive = trend > 0
        //                }

        //            }
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}
        public async Task<OrderChartVM> Income(TimeIntervals? time = null, PeriodSearch? search = null)
        {
            try
            {
                double trend = 0;
                decimal currentIncome = 0;
                decimal previousIncome = 0;

                var( currentStart, currentEnd, previousStart, previousEnd )= DifferRanges(time,search);




                // Income calculation

                currentIncome = await GetIncomeAsync(currentStart, currentEnd);
                previousIncome = await GetIncomeAsync(previousStart, previousEnd);

                if (previousIncome != 0)
                    trend = (double)(currentIncome - previousIncome) / (double)previousIncome * 100;

                // ------------------------------
                // Response
                // ------------------------------
                return new OrderChartVM
                {
                    response = new Dictionary<string, OrderChartPVM>
                    {
                        ["Income"] = new OrderChartPVM
                        {
                            trend = Math.Round(trend, 2),
                            value = (double?)Math.Round(currentIncome, 2),
                            isPositive = trend >= 0
                        }
                    }
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        //Returns the Ratio between all users and users purchesed
        public async Task<OrderChartVM> ConversionRate(TimeIntervals? time = null, PeriodSearch? search = null)
        {
            try
            {
                double trend = 0;
                int currentDifference = 0;
                int previousDifference = 0;

                var (currentStart, currentEnd, previousStart, previousEnd) = DifferRanges(time, search);


                var successfulOrders = await SuccessfullOrder();

                var currentSuccessfulOrders = successfulOrders
                    .Where(x => x.CreatedOn >= currentStart && x.CreatedOn < currentEnd)
                    .ToList();

                var previousSuccessfulOrders = successfulOrders
                    .Where(x => x.CreatedOn >= previousStart && x.CreatedOn < previousEnd)
                    .ToList();

                var currentCustomers = currentSuccessfulOrders.Select(x => x.CustomerId).Distinct().ToList();
                var previousCustomers = previousSuccessfulOrders.Select(x => x.CustomerId).Distinct().ToList();

                var users = await GetUsers();

                var currentUsers = users
                    .Where(x => x.CreationDate >= currentStart && x.CreationDate < currentEnd)
                    .Select(x => x.Id)
                    .ToList();

                var previousUsers = users
                    .Where(x => x.CreationDate >= previousStart && x.CreationDate < previousEnd)
                    .Select(x => x.Id)
                    .ToList();

                //currentDifference =currentUsers.Except(currentCustomers).Count();
                //previousDifference = previousUsers.Except(previousCustomers).Count();
                double currentConvertionRate = ((double)currentCustomers.Count() / currentUsers.Count()) * 100;
                 double PrevConvertionRate = ((double)previousCustomers.Count() / previousUsers.Count()) * 100;

                if (PrevConvertionRate == 0)
                {
                    trend = 0;
                }
                else
                {
                    trend = ((double)(currentConvertionRate - PrevConvertionRate) / PrevConvertionRate) * 100;
                }


                return new OrderChartVM
                {
                    response = new Dictionary<string, OrderChartPVM>
                    {
                        ["ConversionRate"] = new OrderChartPVM
                        {
                            trend = Math.Round(trend, 2),
                            value = ((double) currentCustomers.Count()/currentUsers.Count())*100,
                            isPositive = trend > 0
                        }
                    }
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
        //Returns All Orders Number
        public async Task<OrderChartVM> TotalOrders(TimeIntervals? time = null, PeriodSearch? search = null)
        {
            try
            {
                double trend = 0;
                int currentDifference = 0;
                int previousDifference = 0;
                var (currentStart, currentEnd, previousStart, previousEnd) = DifferRanges(time, search);



                var SuccessfullOrders = await SuccessfullOrder();

                var CurrentPurchase = SuccessfullOrders.Where(x => x.CreatedOn >= currentStart && x.CreatedOn < currentEnd).Count();
                var PrevPurchase = SuccessfullOrders.Where(x => x.CreatedOn >= previousStart && x.CreatedOn < previousEnd).Count();


                if (PrevPurchase != 0)

                    trend = (double)(CurrentPurchase - PrevPurchase) / (double)PrevPurchase * 100;

                return new OrderChartVM
                {
                    response = new Dictionary<string, OrderChartPVM>
                    {
                        ["TotalOrders"] = new OrderChartPVM
                        {
                            trend = trend,
                            value = CurrentPurchase,
                            isPositive = trend > 0
                        }

                    }
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        //Returns Abandoned Carts Rate
        public async Task<OrderChartVM> AbondomentRate(TimeIntervals? time = null, PeriodSearch? search = null)
        {
            try
            {
                double CurrentAbandoned_CartsTotal = 0;
                double PrevAbandoned_CartsTotal = 0;
                double trend = 0;
                int currentDifference = 0;
                int previousDifference = 0;
                var (currentStart, currentEnd, previousStart, previousEnd) = DifferRanges(time, search);

                var carts = await getCarts();
                var CurrentcartsInPeriod = carts.Where(x => x.CreatedOn >= currentStart && x.CreatedOn < currentEnd).Count();
                var PrevicartsInPeriod = carts.Where(x => x.CreatedOn >= previousStart && x.CreatedOn < previousEnd).Count();

                var SuccessfullOrders = await SuccessfullOrder();

                var CurrentSuccessfullOrders = SuccessfullOrders.Where(x => x.CreatedOn >= currentStart && x.CreatedOn < currentEnd).Count();
                var PrevSuccessfullOrders = SuccessfullOrders.Where(x => x.CreatedOn >= previousStart && x.CreatedOn < previousEnd).Count();


                if (CurrentcartsInPeriod != 0)
                    CurrentAbandoned_CartsTotal = CurrentcartsInPeriod > 0 ? (((double)CurrentcartsInPeriod - CurrentSuccessfullOrders )/ CurrentcartsInPeriod) * 100 : 0;

                if (PrevicartsInPeriod != 0)
                    PrevAbandoned_CartsTotal = PrevicartsInPeriod > 0 ? (((double)PrevicartsInPeriod - PrevSuccessfullOrders) / PrevicartsInPeriod) * 100 : 0;

                trend = PrevAbandoned_CartsTotal > 0 ? (double)(CurrentAbandoned_CartsTotal - PrevAbandoned_CartsTotal) / (double)PrevAbandoned_CartsTotal * 100 : 0;

                if (PrevicartsInPeriod != 0)
                    PrevAbandoned_CartsTotal = PrevicartsInPeriod > 0 ? (((double)PrevicartsInPeriod - PrevSuccessfullOrders) / PrevicartsInPeriod) * 100 : 0;

                trend = PrevAbandoned_CartsTotal > 0 ? (double)(CurrentAbandoned_CartsTotal - PrevAbandoned_CartsTotal) / (double)PrevAbandoned_CartsTotal * 100 : 0;
                return new OrderChartVM
                {
                    response = new Dictionary<string, OrderChartPVM>
                    {
                        ["AbandonmentRate"] = new OrderChartPVM
                        {
                            trend = trend,
                            value = CurrentAbandoned_CartsTotal,
                            isPositive = trend > 0
                        }

                    }
                };
            }
          
            catch (Exception ex)

            {
                throw;
            }

        }
        //Returns the most searched product
        public async Task<List<MostSearchedVM>> MostSearchedProducts(TimeIntervals? time = null, PeriodSearch? search = null)

        {

            try

            {

                var (currentStart, currentEnd, previousStart, previousEnd) = DifferRanges(time, search);




                var query = await _queryRepository.Query()

                    .Where(x => x.CreatedOn >= currentStart && x.CreatedOn < currentEnd)

                    .GroupBy(x => x.QueryText ?? "")

                    .Select(g => new MostKeywordsVM { Keyword = g.Key, Count = g.Count() })

                    .ToListAsync();

                var sucseefullOrders = await SuccessfullOrder();
                sucseefullOrders = sucseefullOrders.Where(x => x.CreatedOn >= currentStart && x.CreatedOn < currentEnd).ToList();

                var products = await _productrepository.Query().Include(x => x.ProductDetails).ToListAsync();



                var SearchedProducts = products.Select(x => new

                {

                    product = x,

                    SearchCount = x.ProductDetails.Sum(d =>

                        query.Where(k => d.Name.StartsWith(k.Keyword, StringComparison.OrdinalIgnoreCase))

                             .Sum(k => k.Count))

                })

                .OrderByDescending(x => x.SearchCount)

                .ToList();

                var totalSearches = SearchedProducts.Sum(x => (double)x.SearchCount);


                int totalSales = sucseefullOrders.SelectMany(x => x.OrderItems).Count();


                var result = SearchedProducts.Take(4).Select(p =>
                {

                    //       var y=     p.SearchCount;
                    int itemsales = sucseefullOrders.SelectMany(x => x.OrderItems).Count(y => y.ProductId == p.product.Id);
                    var matchednames = p.product.ProductDetails.Where(d => query.Any(k => d.Name.StartsWith(k.Keyword, StringComparison.OrdinalIgnoreCase))).Select(x => x.Name).FirstOrDefault();
                    return new MostSearchedVM
                    {

                        productId = p.product.Id,

                        productName = matchednames,//p.product.ProductDetails.FirstOrDefault()?.Name ?? "",
                        popularity = totalSearches == 0 ? 0 : (p.SearchCount / totalSearches)*100,

                        salesPercentage = totalSales == 0 ? 0 : ((double)itemsales / totalSales) * 100

                    };

                }).ToList();

                return result;

            }

            catch (Exception ex)

            {

                throw;

            }

        }


        public async Task<DashboardDto> CustomerAnalyticsDashboard(TimeIntervals? time = null, PeriodSearch? search = null)

        {
            try
            {
                DashboardDto result = new DashboardDto();
                var (currentStart, currentEnd, previousStart, previousEnd) = DifferRanges(time, search);

                var succseefullOrder = await SuccessfullOrder();
                succseefullOrder = succseefullOrder.Where(x => x.CreatedOn >= currentStart && x.CreatedOn < currentEnd).ToList();

                var CustomersInPeriod = succseefullOrder.Select(x => x.Customer).Distinct().ToList();

                //Calculate all Customers CLV
                var all_customers_clv = GetCLVAvg(succseefullOrder, CustomersInPeriod, time.HasValue ? time : search.time, DateTime.UtcNow);

                //Calculate top 20% customers CLV
                var sortedCusOrders = succseefullOrder.OrderByDescending(x => x.OrderTotal).ToList();
                var top_customers_count = (int)Math.Ceiling(sortedCusOrders.Count * 0.2);
                var sorted_top_cus = sortedCusOrders.Select(x => x.Customer).Distinct().Take(top_customers_count).ToList();
                var top_twentie_clv = GetCLVAvg(succseefullOrder, sorted_top_cus, time.HasValue ? time : search.time, DateTime.UtcNow);


                //Calculate CLV of customers with 1-3 order
                var CustomerOrdersDic = succseefullOrder.GroupBy(x => x.CustomerId).ToDictionary(g => g.Key, g => g.ToList());
                var casualCustomersDic = CustomerOrdersDic.Where(dic => dic.Value.Count >= 1 && dic.Value.Count <= 3).ToDictionary(dic => dic.Key, dic => dic.Value);
                var casualCustomers = succseefullOrder.Where(x => casualCustomersDic.ContainsKey(x.CustomerId)).Distinct().Select(x => x.Customer).ToList();
                var causalCustomersCLV = GetCLVAvg(succseefullOrder, casualCustomers, time.HasValue ? time : search.time, DateTime.UtcNow);


                //Calculate CLV of Prime Only Buyers
                var PrimeOnlyBuyers = succseefullOrder.Where(x => x.DiscountAmount > 0).Select(x => x.Customer).Distinct().ToList();
                var PrimeOnlyBuyersCLV = GetCLVAvg(succseefullOrder, PrimeOnlyBuyers, time.HasValue ? time : search.time, DateTime.UtcNow);


                // all customers
                var PrevUser = succseefullOrder.Where(x => x.CreatedOn >= previousStart && x.CreatedOn < previousEnd).Select(x => x.Customer).ToList();
                var trend_percentage_allCustomers = PrevUser.Count() > 0 ? ((CustomersInPeriod.Count() - PrevUser.Count()) / PrevUser.Count() * 100) : 0;



                //return guest
                var GuestgroupIds = await _customerGroupUserRepository.Query().Where(x => x.CustomerGroupId == 2).Select(x => x.UserId).ToListAsync();
                var Current_buyers_guests = CustomersInPeriod.Where(x => GuestgroupIds.Contains(x.Id)).Count();
                var prev_buyers_guests = PrevUser.Where(x => GuestgroupIds.Contains(x.Id)).Count();
                var trend_percentag_guest = prev_buyers_guests > 0 ? ((Current_buyers_guests - prev_buyers_guests) / prev_buyers_guests * 100) : 0;

                //Inactive
                var currentInactiveUsers = CustomersInPeriod.Where(x => x.Status == EStatus.Inactive).Count();
                var prevInactiveUsers = PrevUser.Where(x => x.Status == EStatus.Inactive).Count();
                var trend_percentage_inactive = prevInactiveUsers > 0 ? ((currentInactiveUsers - prevInactiveUsers) / prevInactiveUsers * 100) : 0;


                //Active
                var currentactiveUsers = CustomersInPeriod.Where(x => x.Status == EStatus.Active).Count();
                var prevactiveUsers = PrevUser.Where(x => x.Status == EStatus.Active).Count();
                var trend_percentage_active = prevactiveUsers > 0 ? ((currentactiveUsers - prevactiveUsers) / prevactiveUsers * 100) : 0;


                //current Retention Rate 
                var start_customer = succseefullOrder.Select(x => x.Customer).Where(x => x.CreationDate == currentStart).Count();
                var end_customer = succseefullOrder.Select(x => x.Customer).Where(x => x.CreationDate == currentEnd).Count();
                var inbetween_customers = succseefullOrder.Select(x => x.Customer).Where(x => x.CreationDate > currentStart && x.CreationDate < currentEnd).Count();
                var currentRetetion = start_customer > 0 ? (((end_customer - inbetween_customers) / start_customer) * 100) : 0;
                //previos Retention Rate
                var P_start_customer = succseefullOrder.Select(x => x.Customer).Where(x => x.CreationDate == previousStart).Distinct().Count();
                var P_end_customer = succseefullOrder.Select(x => x.Customer).Where(x => x.CreationDate == previousEnd).Distinct().Count();
                var P_inbetween_customers = succseefullOrder.Select(x => x.Customer).Where(x => x.CreationDate > previousStart && x.CreationDate < previousEnd).Distinct().Count();
                var P_Retetion = P_start_customer > 0 ? (((P_end_customer - P_inbetween_customers) / P_start_customer) * 100) : 0;

                var trend_retetion = P_Retetion > 0 ? ((currentRetetion - P_Retetion) / P_Retetion * 100) : 0;


                //churn rate
                var prevcustomers = succseefullOrder.Where(x => x.CreatedOn > previousStart && x.CreatedOn < previousEnd).Select(x => x.Customer).Distinct().ToList();
                var currentcustomer = succseefullOrder.Where(x => x.CreatedOn > currentStart && x.CreatedOn < currentEnd).Select(x => x.Customer).Distinct().ToList();
                var lostCustomer = prevcustomers.Except(currentcustomer).Count();

                var current_churn = start_customer > 0 ? ((lostCustomer / start_customer) * 100) : 0;
                //prev chart rate 
                var (p_previosStart, p_previosEnd) = GetDateRange(time.HasValue ? time : search.time, DateTime.UtcNow, 2);


                var p_prevcustomers = succseefullOrder.Where(x => x.CreatedOn > p_previosStart && x.CreatedOn < p_previosEnd).Select(x => x.Customer).Distinct().ToList();
                var p_currentcustomer = succseefullOrder.Where(x => x.CreatedOn > previousStart && x.CreatedOn < previousEnd).Select(x => x.Customer).Distinct().ToList();
                var p_lostCustomer = p_prevcustomers.Except(p_currentcustomer).Count();
                var prev_churn = P_start_customer > 0 ? ((p_lostCustomer / P_start_customer) * 100) : 0;

                var trend_churm = prev_churn > 0 ? ((current_churn - prev_churn) / prev_churn * 100) : 0;

                var intervals = GetDynamicIntervals(time, search, currentStart, currentEnd);

                var chartData = new List<ChartDto>();



                foreach (var (start, end) in intervals)

                {

                    var ordersInRange = succseefullOrder.Where(x => x.CreatedOn >= start && x.CreatedOn < end).ToList();

                    var customersInRange = ordersInRange.Select(x => x.Customer).Distinct().ToList();


                    decimal clv_all = 0, clv_top = 0, clv_casual = 0, clv_promo = 0;
                    if (ordersInRange.Any())

                    {



                        clv_all = GetCLVAvg(ordersInRange, customersInRange, time.HasValue ? time : search.time, DateTime.UtcNow);



                        var sortedOrders = ordersInRange.OrderByDescending(x => x.OrderTotal).ToList();

                        var topCount = (int)Math.Ceiling(sortedOrders.Count * 0.2);

                        var topCus = sortedOrders.Select(x => x.Customer).Distinct().Take(topCount).ToList();

                        clv_top = GetCLVAvg(ordersInRange, topCus, time.HasValue ? time : search.time, DateTime.UtcNow);



                        var customerOrdersDic = ordersInRange.GroupBy(x => x.CustomerId).ToDictionary(g => g.Key, g => g.ToList());

                        var casualDic = customerOrdersDic.Where(d => d.Value.Count >= 1 && d.Value.Count <= 3)

                                                         .ToDictionary(d => d.Key, d => d.Value);

                        var casuals = ordersInRange.Where(x => casualDic.ContainsKey(x.CustomerId)).Select(x => x.Customer).Distinct().ToList();

                        clv_casual = GetCLVAvg(ordersInRange, casuals, time.HasValue ? time : search.time, DateTime.UtcNow);



                        var promoOnly = ordersInRange.Where(x => x.DiscountAmount > 0).Select(x => x.Customer).Distinct().ToList();

                        clv_promo = GetCLVAvg(ordersInRange, promoOnly, time.HasValue ? time : search.time, DateTime.UtcNow);


                    }

                    chartData.Add(new ChartDto

                    {

                        date = (DateTime)start,

                        clv_value = clv_all,

                        high_spenders = clv_top,

                        casual_shoppers = clv_casual,

                        prime_only_buyers = clv_promo

                    });

                }



                result.clv_chart = new ClvChartDto

                {

                    period = (time.HasValue ? time : search.time).ToString(),


                    chart_data = chartData

                };



                result.allCustomers =
                    new CustomersDto
                    {
                        count = CustomersInPeriod.Count(),
                        trend_percentage = trend_percentage_allCustomers,
                        trend_direction = trend_percentage_allCustomers <= 0 ? "down" : "Up"
                    };

                result.guest_users =
                  new CustomersDto
                  {
                      count = Current_buyers_guests,
                      trend_percentage = trend_percentag_guest,
                      trend_direction = trend_percentag_guest <= 0 ? "down" : "Up"
                  };
                result.inActive_Customer =
                new CustomersDto
                {
                    count = currentInactiveUsers,
                    trend_percentage = trend_percentage_inactive,
                    trend_direction = trend_percentage_inactive <= 0 ? "down" : "Up"
                };
                result.active_Customers =
                  new CustomersDto
                  {
                      count = currentactiveUsers,
                      trend_percentage = trend_percentage_active,
                      trend_direction = trend_percentage_active <= 0 ? "down" : "Up"
                  };

                result.retention_rate =
                    new RateMetricDto
                    {
                        percentage = currentRetetion,
                        trend_percentage = trend_retetion,
                        trend_direction = trend_retetion <= 0 ? "down" : "Up"
                    };

                result.churn_rate =
                    new RateMetricDto
                    {
                        percentage = trend_churm,
                        trend_percentage = trend_churm,
                        trend_direction = trend_churm <= 0 ? "down" : "Up"
                    };

                return result;


            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<CartOrderChart>> GetCartOrderCharts(TimeIntervals? time = null, PeriodSearch? search = null)
        {
            List<CartOrderChart> cart_order_charts = new();

            try
            {
                var (currentStart, currentEnd, previousStart, previousEnd) = DifferRanges(time, search);


                var cartsInPeriod = await _cartRepository.Query().Where(x => x.CreatedOn >= currentStart && x.CreatedOn < currentEnd).ToListAsync();
                var SuccessfullOrders = await SuccessfullOrder();
                SuccessfullOrders = SuccessfullOrders.Where(x => x.CreatedOn > currentStart && x.CreatedOn < currentEnd).ToList();
                //Filter through carts that haven't been purchased within the selected period
                var Abandoned_CartsTotal = cartsInPeriod.Count(x => !SuccessfullOrders.Any(y => y.CartId == x.Id));

                var OrderInPeriod = await _orderrepository.Query().Where(x => x.CreatedOn >= currentStart && x.CreatedOn < currentEnd).ToListAsync();

                var GroupOrdersByCarts = OrderInPeriod.GroupBy(x => x.CartId);

                //Filter through Carts that have been abondend then get purchased within the selected period but not in the same day
                var successStatuses = new[]
   {
    OrderStatus.CashOnDelivery,
    OrderStatus.PaymentReceived,
    OrderStatus.PaidByCoupon,
    OrderStatus.Complete
};

                var Recovered_CartsTotal = GroupOrdersByCarts.Count(cart =>
                    // Has at least one successful order
                    cart.Any(o => successStatuses.Contains(o.OrderStatus))
                    // Has at least one non-successful order
                    && cart.Any(o => !successStatuses.Contains(o.OrderStatus))
                    // Earliest unsuccessful order is before the latest successful order
                    && cart.Where(o => !successStatuses.Contains(o.OrderStatus)).Min(o => o.CreatedOn)
                       < cart.Where(o => successStatuses.Contains(o.OrderStatus)).Max(o => o.CreatedOn)
                );

                //calculate Recovery_Rate
                var Cart_Recovery_Rate = Abandoned_CartsTotal == 0 ? 0 : (double)Recovered_CartsTotal / Abandoned_CartsTotal * 100;

                cart_order_charts.Add(new CartOrderChart { label = "Abandoned Carts", value = Abandoned_CartsTotal });
                cart_order_charts.Add(new CartOrderChart { label = "Recovered Carts", value = Recovered_CartsTotal });
                cart_order_charts.Add(new CartOrderChart { label = "Recovery Rate (%)", value = Cart_Recovery_Rate });

                return cart_order_charts;


            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<TrafficSourceVM>> AbandonmentByTrafficSource(TimeIntervals? time = null, PeriodSearch? search = null)
        {
            try
            {

                var (currentStart, currentEnd, previousStart, previousEnd) = DifferRanges(time, search);


                // Filter on the DB instead of loading the whole Analytics_TrafficSource table
                // then filtering in memory; returns the identical row set for the grouping below.
                var CurrentTraffic = await _trafficRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.CreationDate >= currentStart && x.CreationDate < currentEnd)
                    .ToListAsync();
                //  var prevTraffic = traffics.Where(x => x.CreationDate >= previosStart && x.CreationDate < previosEnd).ToList();
                var SuccsessfullOrders = await SuccessfullOrder();
                var SuccsessfullCartIds = SuccsessfullOrders.Select(x => x.CartId).ToHashSet();

                var GroupedTraffic = CurrentTraffic.GroupBy(k => k.TrafficSource).Select(g => new TrafficSourceVM()
                {
                    TrafficSource = g.Key,
                    //TotalCart=g.Count(),
                    // SuccsessfullOrder=g.Count(t=>SuccsessfullCartIds.Contains(t.Id)),
                    AbondmentRate = (double)(g.Count() - g.Count(t => SuccsessfullCartIds.Contains(t.CartId)) / g.Count()) * 100

                }).OrderByDescending(x => x.AbondmentRate)
                .ToList();

                return GroupedTraffic;


            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<List<TrafficDeviceVM>> AbandonmentByDevice(TimeIntervals? time = null, PeriodSearch? search = null)
        {
            try
            {
                var (currentStart, currentEnd, previousStart, previousEnd) = DifferRanges(time, search);

                //  var (previosStart, previosEnd) = GetDateRange(time, DateTime.UtcNow, 1);
                //    var traffics = await _trafficRepository.Query().ToListAsync();
                //  var CurrentTraffic = traffics.Where(x => x.CreationDate >= currentStart && x.CreationDate < currentEnd).ToList();
                //  var prevTraffic = traffics.Where(x => x.CreationDate >= previosStart && x.CreationDate < previosEnd).ToList();
                var SuccsessfullOrders = await SuccessfullOrder();

                var allOrders = await _orderrepository.Query().Where(x => x.CreatedOn >= currentStart && x.CreatedOn < currentEnd).ToListAsync();
                var SuccsessfullCartIds = SuccsessfullOrders.Where(x => x.CreatedOn > currentStart && x.CreatedOn < currentEnd).Select(x => x.CartId).ToHashSet();

                var GroupedTraffic = allOrders.GroupBy(k => k.Channel).Select(g => new TrafficDeviceVM()
                {
                    channel = g.Key,
                    //TotalCart=g.Count(),
                    // SuccsessfullOrder=g.Count(t=>SuccsessfullCartIds.Contains(t.Id)),
                    AbondmentRate = (double)(g.Count() - g.Count(t => SuccsessfullCartIds.Contains(t.CartId)) / g.Count()) * 100

                }).OrderByDescending(x => x.AbondmentRate)
                .ToList();

                return GroupedTraffic;


            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<SalesFunnalVM>> SalesFunnel(filterType filter= filterType.Defualt, TimeIntervals? time = null, PeriodSearch search = null)
        {
            try

            {
                var (currentStart, currentEnd, previousStart, previousEnd) = DifferRanges(time, search);
                // Get your page info JSON

                var appSettingSlugs = await _appSettingRepository.Query()

                    .Where(x => x.Id.Contains("SalesFunnel"))
                      .Select(x => x.Value)
                     .FirstAsync();



                //Deserialize JSON into pairs

                var pageInfo = JsonConvert.DeserializeObject<List<List<PageInfo>>>(appSettingSlugs);

                // user navigations across all users
                var allNavigations = await _uservisistsRepository.Query()
                .Where(x => x.CreationDate >= currentStart && x.CreationDate <= currentEnd)
                     .SelectMany(x => x.UserNavigations)
                    .ToListAsync();


                var slugMap = pageInfo
                    .SelectMany(pair => pair.Select(p => new

                    {

                        p.Name,

                        p.Id,

                        EnglishName = pair.FirstOrDefault(x => IsEnglish(x.Name))?.Name ?? p.Name

                    }))

                    .ToList();

                // Group visits by slug, match them to their PageId, and count

                var visitsPerSlug = allNavigations

                    .GroupBy(nav => nav.Slug)

                    .Select(g =>

                    {

                        var match = slugMap.FirstOrDefault(x =>

                            x.Name.Equals(g.Key, StringComparison.OrdinalIgnoreCase));



                        return new

                        {

                            Slug = g.Key,

                            PageId = match?.Id,

                            EnglishName = match?.EnglishName,

                            Count = g.Count()

                        };

                    })

                    .Where(x => x.PageId != null)

                    .ToList();


                var pageVisits = visitsPerSlug

                    .GroupBy(x => new { x.PageId, x.EnglishName })

                    .Select(g => new SalesFunnalVM

                    {

                        Name = g.Key.EnglishName,

                        userCount = g.Sum(x => x.Count),
                        // dropOffPercentage=

                    })

                    .OrderByDescending(x => x.userCount)
                    .ToList();
                if (filter == filterType.DropOff)
                {
                    for (int i = 0; i < pageVisits.Count; i++)
                    {
                        if (i == 0)
                            pageVisits[i].dropOffPercentage = 0;
                        else
                        {
                            int cuurent = pageVisits[i].userCount;
                            int previous = pageVisits[i - 1].userCount;
                            double dropoffper = Math.Round(((double)(previous - cuurent) / previous) * 100, 2);
                            pageVisits[i].dropOffPercentage = dropoffper;
                        }
                    }
                }


                return pageVisits;

            }

            catch (Exception ex)
            { 
                  throw;

            }






            bool IsEnglish(string text) => text.All(c => c < 128);


        }

       public async Task<List<SourceClicks>> ClickThroughRate(TimeIntervals? time = null, PeriodSearch? search = null)

{

    try

    {

        var (currentStart, currentEnd, previousStart, previousEnd) = DifferRanges(time, search);



        // Get all newsletter statistics in the range

        var newsLettersStatistics = await _newsViewsRepository.Query()
             .Include(x=>x.NewsLetterStatistic)

            .ThenInclude(x => x.NewsLetter)

            .Where(x => x.CreationDate >= currentStart && x.CreationDate < currentEnd)

            .Select(x => x.NewsLetterStatistic.NewsLetter)

            .ToListAsync();



        // Group by notification type with counts

        var notificationGroups = newsLettersStatistics

            .GroupBy(x => x.NotificationType)

            .ToDictionary(g => g.Key, g => g.Count());


                double TotalClicks = notificationGroups.Sum(g => g.Value);

        var allNotificationTypes = new List<NotificationType>

        {

            NotificationType.Email,

            NotificationType.SMS,

            NotificationType.PushNotification

        }

        .Select(nt => new SourceClicks

        {

            Name = nt.ToString(),

            NumberOfClicks = (int)(notificationGroups.ContainsKey((EskaCMS.Entities.Enum.GeneralEnums.CoreNotificationType)nt) ? TotalClicks>0?((double)notificationGroups[(EskaCMS.Entities.Enum.GeneralEnums.CoreNotificationType)nt]/ TotalClicks)*100 :0 : 0)

        })

        .ToList();



        return allNotificationTypes;

    }

    catch

    {

        throw;

    }

}

        public async Task<List<SourceClicks>> ProductTrafficSource(TimeIntervals? time = null, PeriodSearch? search = null)
        {
            try
            {
                var (currentStart, currentEnd, previousStart, previousEnd) = DifferRanges(time, search);

                var recentlyViewed = await _recentlyViewdProduct.Query().Where(x => x.LatestViewedOn >= currentStart && x.LatestViewedOn < currentEnd).ToListAsync();
                var SourceGroup = recentlyViewed

          .GroupBy(x => x.Source)

          .ToDictionary(g => g.Key??SourceTraffic.None, g => g.Count());


                double TotalSearches = SourceGroup.Sum(g => g.Value);
                

                var All = new List<SourceTraffic>

        {

            SourceTraffic.None,

            SourceTraffic.SocialMedia,

            SourceTraffic.Directraffic,
            SourceTraffic.PaidSearch,
            SourceTraffic.OrganicSearch

        }

                .Select(nt => new SourceClicks

                {

                    Name = nt.ToString(),

                    NumberOfClicks = (int)(SourceGroup.ContainsKey(nt) ? TotalSearches > 0 ? ((double)SourceGroup[nt] / TotalSearches) * 100 : 0 : 0)

                })

                .ToList();
                return All;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<ProductViewsVM>> Product_Views(int? productId,TimeIntervals? time = null, PeriodSearch? search = null)
        {
            try
            {
                List<ProductViewsVM> viewsChart = new List<ProductViewsVM>();
                var (currentStart, currentEnd, previousStart, previousEnd) = DifferRanges(time, search);
                var intervals = GetDynamicIntervals(time, search, currentStart, currentEnd);
                var views = productId!=0? await _recentlyViewdProduct.Query().Where(x => x.ProductId == productId).ToListAsync(): await _recentlyViewdProduct.Query().ToListAsync();
               
                foreach (var (start, end) in intervals)
                {
                    viewsChart.Add(new ProductViewsVM()
                    {
                        date = start,
                        NumberOfViews = views.Where(x => x.LatestViewedOn >= start && x.LatestViewedOn < end).Count()
                    });
                }
                return viewsChart;
            }
            catch(Exception ex)
            {
                throw;
            }

        }

        public async Task<ProductCardsVM> product_insights(TimeIntervals? time = null, PeriodSearch? search = null)
        {
            try
            {
                var (currentStart, currentEnd, previousStart, previousEnd) = DifferRanges(time, search);
                //Revenue
                ProductCardsVM results = new ProductCardsVM
                {
                    cardDetailsVM = new Dictionary<string, CardDetailsVM>()
                };
                var Orders = await SuccessfullOrder();
                  //Current
                decimal CurentRevenue = Orders.Where(x=>x.CreatedOn>=currentStart&&x.CreatedOn<currentEnd).SelectMany(x => x.OrderItems).Sum(x => x.ProductPrice*x.Quantity);
                  //Prev
                decimal PrevRevenue = Orders.Where(x => x.CreatedOn >= previousStart && x.CreatedOn < previousEnd).SelectMany(x => x.OrderItems).Sum(x => x.ProductPrice * x.Quantity);
                  //trend
                double trendRevenue= PrevRevenue>0?(double)((CurentRevenue- PrevRevenue)/ PrevRevenue)*100:0;

                results.cardDetailsVM.Add(
                    "Revenue",
                        new CardDetailsVM
                        {
                            Value =(double) CurentRevenue,
                            Percentage = trendRevenue
                        });
                
                //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------

                // Drop-Off Rate
                //Cuurent
                var AllOrders = await _orderrepository.Query().Include(x=>x.Customer).ToListAsync();
                var SuccessfullProuctOrders = Orders.Where(x => x.CreatedOn >= currentStart && x.CreatedOn < currentEnd).SelectMany(x => x.OrderItems).Select(x => x.ProductId).Count();

                var CurrentAllProducts=AllOrders.Where(x => x.CreatedOn >= currentStart && x.CreatedOn < currentEnd).SelectMany(x => x.OrderItems).Select(x => x.ProductId).Count();

                double CurrentDropOffRate = CurrentAllProducts>0?((double)(CurrentAllProducts - SuccessfullProuctOrders) / CurrentAllProducts) *100:0;
                 //Previos
                var PreviosAllProducts = AllOrders.Where(x => x.CreatedOn >= previousStart && x.CreatedOn < previousEnd).SelectMany(x => x.OrderItems).Select(x => x.ProductId).Count();
                var Pre_SuccessfullProuctOrders = Orders.Where(x => x.CreatedOn >= previousStart && x.CreatedOn < previousEnd).SelectMany(x => x.OrderItems).Select(x => x.ProductId).Count();

                double PreviosDropOffRate = PreviosAllProducts>0?((double)(PreviosAllProducts - Pre_SuccessfullProuctOrders) / PreviosAllProducts) * 100:0;
                //trend
                var TrendDropOff = PreviosDropOffRate > 0 ? ((double)(CurrentDropOffRate - PreviosDropOffRate) / PreviosDropOffRate) * 100 : 0;


                results.cardDetailsVM.Add(
                    "Drop-Off Rate",
                        new CardDetailsVM
                        {
                            Value = CurrentDropOffRate,
                            Percentage = TrendDropOff
                        });
                //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------

                //Lead Convertion Rate
                var AllUsers = await _usersrepository.Query().ToListAsync();
                  //Current
                var CurrentNoCustomer=AllUsers.Where(x=>x.Type== EUsersTypes.Customer&&x.CreationDate>=currentStart&&x.CreationDate<currentEnd).Select(x => x.Id).Count();
                var CurentPurchedCust= Orders.Where(x => x.Customer.Type == EUsersTypes.Customer && x.Customer.CreationDate >= currentStart && x.Customer.CreationDate < currentEnd).Select(x => x.CustomerId).Distinct().Count();
                var Current_ConvartionRate = CurrentNoCustomer>0?((double) CurentPurchedCust / CurrentNoCustomer) * 100:0;

                  //Previos
                var PrevNoCustomer = AllUsers.Where(x => x.Type == EUsersTypes.Customer && x.CreationDate >= previousStart && x.CreationDate < previousEnd).Select(x => x.Id).Count();
                var PrevPurchedCust = Orders.Where(x => x.Customer.Type == EUsersTypes.Customer && x.Customer.CreationDate >= previousStart && x.Customer.CreationDate < previousEnd).Select(x => x.CustomerId).Distinct().Count();
                var Previos_ConvartionRate = PrevNoCustomer>0?((double)PrevPurchedCust / PrevNoCustomer) * 100:0;
                   //trend
                double trendConvRate= Previos_ConvartionRate>0?(double)(Current_ConvartionRate-Previos_ConvartionRate)/ Previos_ConvartionRate*100:0;

                results.cardDetailsVM.Add(
                        "Lead convertion rate",
                            new CardDetailsVM
                            {
                                Value = Current_ConvartionRate,
                                Percentage = trendConvRate
                            });
                //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------

                //Convertion Rate Time
                //current
                var allCustomer = AllUsers.Where(x =>( x.Type == EUsersTypes.Customer||x.Type==EUsersTypes.GuestClient) && x.CreationDate >= currentStart && x.CreationDate < currentEnd).Select(x => x.Id).Count();
                var CurentPurchedLead = Orders.Where(x => (x.Customer.Type == EUsersTypes.Customer || x.Customer.Type == EUsersTypes.GuestClient)
                && x.Customer.CreationDate >= currentStart && x.Customer.CreationDate < currentEnd)
                    .GroupBy(g => g.Id)
                    .Select(y => new
                    {
                        CustomerId = y.Key,
                        CustomerCreation = y.First().Customer.CreationDate,
                        DateOfPurchase = y.Min(Order => Order.CreatedOn)
                    }).Sum(c=>(c.CustomerCreation-c.DateOfPurchase).TotalDays);

                var currentConvTime = ((double)CurentPurchedLead / allCustomer) * 100;

                //Prev
                var PrevallCustomer = AllUsers.Where(x => (x.Type == EUsersTypes.Customer || x.Type == EUsersTypes.GuestClient) && x.CreationDate >= previousStart && x.CreationDate < previousEnd).Select(x => x.Id).Count();
                var PrevPurchedLead = Orders.Where(x => (x.Customer.Type == EUsersTypes.Customer || x.Customer.Type == EUsersTypes.GuestClient)
                && x.Customer.CreationDate >= previousStart && x.Customer.CreationDate < previousEnd)
                    .GroupBy(g => g.Id)
                    .Select(y => new
                    {
                        CustomerId = y.Key,
                        CustomerCreation = y.First().Customer.CreationDate,
                        DateOfPurchase = y.Min(Order => Order.CreatedOn)
                    }).Sum(c => (c.CustomerCreation - c.DateOfPurchase).TotalDays);

                var PrevConvTime = ((double)CurentPurchedLead / allCustomer) * 100;

                  //trend
                double trenConvTime = PrevConvTime>0?((double)(currentConvTime - PrevConvTime) / PrevConvTime) * 100:0;

                results.cardDetailsVM.Add(
                                   "Avg Convertion Time",
                                       new CardDetailsVM
                                       {
                                           Value = Current_ConvartionRate,
                                           Percentage = trendConvRate
                                       });
                //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------



                return results;




            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private List<(DateTime start, DateTime end)> SplitByInterval(TimeIntervals time, DateTime currentStart, DateTime currentEnd)

        {

            List<(DateTime, DateTime)> intervals = new List<(DateTime, DateTime)>();



            switch (time)

            {

                case TimeIntervals.Yearly:

                    // split by months

                    for (var date = currentStart; date < currentEnd; date = date.AddMonths(1))

                    {

                        var end = date.AddMonths(1) > currentEnd ? currentEnd : date.AddMonths(1);

                        intervals.Add((date, end));

                    }

                    break;



                case TimeIntervals.Monthly:

                    // split by weeks

                    for (var date = currentStart; date < currentEnd; date = date.AddDays(7))

                    {

                        var end = date.AddDays(7) > currentEnd ? currentEnd : date.AddDays(7);

                        intervals.Add((date, end));

                    }

                    break;



                case TimeIntervals.Weekly:

                    // split by days

                    for (var date = currentStart; date < currentEnd; date = date.AddDays(1))

                    {

                        var end = date.AddDays(1) > currentEnd ? currentEnd : date.AddDays(1);

                        intervals.Add((date, end));

                    }

                    break;

            }



            return intervals;

        }


        private (DateTime start, DateTime end) GetDateRange(TimeIntervals? period, DateTime referenceDate, int offset = 0)
        {
            try
            {
                (DateTime start, DateTime end) range = period switch
                {
                    TimeIntervals.Weekly =>
                    (
                        referenceDate.AddDays(-(int)referenceDate.DayOfWeek), // start of current week
                        referenceDate.AddDays(6 - (int)referenceDate.DayOfWeek) // end of current week
                    ),
                    TimeIntervals.Monthly =>
                    (
                        new DateTime(referenceDate.Year, referenceDate.Month, 1), // start of current month
                        new DateTime(referenceDate.Year, referenceDate.Month, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month)) // end of current month
                    ),
                    TimeIntervals.Yearly =>
                    (
                        new DateTime(referenceDate.Year, 1, 1), // start of current year
                        new DateTime(referenceDate.Year, 12, 31) // end of current year
                    ),
                    _ => throw new ArgumentOutOfRangeException(nameof(period), period, null)
                };

                if (offset != 0)
                {
                    range = period switch
                    {
                        TimeIntervals.Weekly => (range.start.AddDays(-7 * offset), range.end.AddDays(-7 * offset)),
                        TimeIntervals.Monthly =>
                        (
                            range.start.AddMonths(-offset),
                            new DateTime(range.start.AddMonths(-offset).Year, range.start.AddMonths(-offset).Month,
                            DateTime.DaysInMonth(range.start.AddMonths(-offset).Year, range.start.AddMonths(-offset).Month))
                        ),
                        TimeIntervals.Yearly => (range.start.AddYears(-offset), range.end.AddYears(-offset)),
                        _ => range
                    };
                }

                return range;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        private async Task<decimal> GetIncomeAsync(DateTime StartDate, DateTime EndDate)
        {
            try
            {
                var Income = await _orderrepository.Query().Where(x => x.CreatedOn >= StartDate && x.CreatedOn < EndDate
                   && (x.OrderStatus == OrderStatus.CashOnDelivery
               || x.OrderStatus == OrderStatus.PaymentReceived
               || x.OrderStatus == OrderStatus.PaidByCoupon
               || x.OrderStatus == OrderStatus.Complete))
                    .Select(x => x.OrderItems.Sum(y => y.ProductPrice * y.Quantity)
                    - x.DiscountAmount)
                    .SumAsync();
                return Income;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<List<Order>> SuccessfullOrder()
        {
            try
            {
                var SuccessfullOrders = await _orderrepository.Query().Include(x => x.Customer).Include(x => x.OrderItems).Where(x =>
                   (x.OrderStatus == OrderStatus.CashOnDelivery
               || x.OrderStatus == OrderStatus.PaymentReceived
               || x.OrderStatus == OrderStatus.PaidByCoupon
               || x.OrderStatus == OrderStatus.Complete))
                  .ToListAsync();
                return SuccessfullOrders;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<List<User>> GetUsers()
        {
            try
            {
                var Users = await _usersrepository.Query().ToListAsync();
                return Users;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<List<Cart>> getCarts()
        {
            try
            {
                var carts = await _cartRepository.Query().ToListAsync();
                return carts;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private List<(DateTime? start, DateTime? end)> GetDynamicIntervals(
    TimeIntervals? time,
    PeriodSearch? search,
    DateTime defaultStart,
    DateTime defaultEnd)

        {

            var intervals = new List<(DateTime? start, DateTime? end)>();



            DateTime? start = defaultStart;

            DateTime? end = defaultEnd;



            // If user provided search parameters (custom date range)

            if (search != null)

            {

                start = search.dateFrom;

                end = search.dateTo;



                switch (search.time)

                {

                    case TimeIntervals.Yearly:

                        for (var date = start; date <= end; date = date?.AddYears(1))

                        {

                            var next = date?.AddYears(1) > end ? end : date?.AddYears(1);

                            intervals.Add((date, next));

                        }

                        break;



                    case TimeIntervals.Monthly:

                        for (DateTime? date = start; date <= end; date = date?.AddMonths(1))

                        {

                            DateTime? next = date?.AddMonths(1) >end ? end : date?.AddMonths(1);

                            intervals.Add((date, next));

                        }

                        break;



                    case TimeIntervals.Weekly:

                        for (DateTime? date = start; date <= end; date = date?.AddDays(7))

                        {

                            DateTime? next = date?.AddDays(7) > end ? end : date?.AddDays(7);

                            intervals.Add((date, next));

                        }

                        break;

                }

            }

            // If user selected only TimeIntervals

            else if (time.HasValue)

            {

                switch (time.Value)

                {

                    case TimeIntervals.Yearly:

                        // Split by months

                        for (DateTime? date = start; date <= end; date = date?.AddMonths(1))

                        {

                            DateTime? next = date?.AddMonths(1) >= end ? end : date?.AddMonths(1);

                            intervals.Add((date, next));

                        }

                        break;



                    case TimeIntervals.Monthly:

                        // Split by weeks

                        for (DateTime? date = start; date <= end; date = date?.AddDays(7))

                        {

                            DateTime? next = date?.AddDays(7) >= end ? end : date?.AddDays(7);

                            intervals.Add((date, next));

                        }

                        break;



                    case TimeIntervals.Weekly:

                        // Split by days

                        for (DateTime? date = start; date <= end; date = date?.AddDays(1))

                        {

                            DateTime? next = date?.AddDays(1) >= end ? end : date?.AddDays(1);

                            intervals.Add((date, next));

                        }

                        break;

                }

            }



            return intervals;

        }

        private decimal GetCLVAvg(List<Order> orders, List<User> Customers, TimeIntervals? time, DateTime currentStart)
        {
            decimal CLV = 0;
            List<decimal> CLVList = new List<decimal>();
            decimal number_of_purchases = 0;
            decimal Avg_Order_Value = 0;
            int customer_life_span = 0;
            var days = time switch
            {
                TimeIntervals.Yearly => 365,
                TimeIntervals.Weekly => 7,
                TimeIntervals.Monthly => 30
            };
            if (Customers.Count() == 0) return 0;
            //Calculate all Customers CLV
            foreach (var customer in Customers)
            {
                number_of_purchases = orders.Where(x => x.CustomerId == customer.Id).Count();

                Avg_Order_Value = orders.Where(x => x.CustomerId == customer.Id).Average(x => x.OrderTotal);


                double diffrence = (currentStart - customer.CreationDate).TotalDays;
                customer_life_span = (int)Math.Round(diffrence / days);
                CLV = number_of_purchases * Avg_Order_Value * customer_life_span;
                CLVList.Add(CLV);
            }

            return CLVList.Average();
        }

        public (DateTime currentStart, DateTime currentEnd, DateTime previousStart, DateTime previousEnd)

    DifferRanges(TimeIntervals? time, PeriodSearch? search)

        {

            DateTime currentStart, currentEnd, previousStart, previousEnd;



            // Option A: Enum-based interval

            if (time.HasValue && search == null)

            {

                (currentStart, currentEnd) = GetDateRange(time, DateTime.UtcNow);

                (previousStart, previousEnd) = GetDateRange(time, DateTime.UtcNow, 1);

            }

            // Option B: Custom range + partition by enum

            else if (search != null && search.dateFrom.HasValue && search.dateTo.HasValue)

            {

                var totalStart = search.dateFrom.Value;

                var totalEnd = search.dateTo.Value;



                switch (search.time)

                {

                    case TimeIntervals.Weekly:

                        currentEnd = totalEnd;

                        currentStart = totalEnd.AddDays(-7);

                        previousEnd = currentStart;

                        previousStart = previousEnd.AddDays(-7);

                        break;



                    case TimeIntervals.Monthly:

                        currentEnd = totalEnd;

                        currentStart = totalEnd.AddMonths(-1);

                        previousEnd = currentStart;

                        previousStart = previousEnd.AddMonths(-1);

                        break;



                    case TimeIntervals.Yearly:

                        currentEnd = totalEnd;

                        currentStart = totalEnd.AddYears(-1);

                        previousEnd = currentStart;

                        previousStart = previousEnd.AddYears(-1);

                        break;



                    default:

                        throw new ArgumentException("Invalid or missing TimeInterval in PeriodSearch.");

                }

            }

            else

            {

                throw new ArgumentException("You must provide either a time interval or a valid PeriodSearch object.");

            }



            return (currentStart, currentEnd, previousStart, previousEnd);

        }


    }
}
