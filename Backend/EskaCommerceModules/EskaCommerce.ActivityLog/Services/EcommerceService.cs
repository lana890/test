using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Orders.Models;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCMS.UserDashboard.Services.Interfaces;
using EskaCMS.UserDashboard.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.UserDashboard.Services
{
    public class EcommerceService : IEcommerceService
    {
        public IRepository<Order> _orderrepository {  get; set; }
        public IRepository<User> _usersrepository {  get; set; }

        public IRepository<Cart> _cartRepository {  get; set; }
        public EcommerceService(
            IRepository<Order> orderRepository,
            IRepository<User> usersrepository,
            IRepository<Cart> cartRepository) {
            _orderrepository = orderRepository;
            _usersrepository = usersrepository;
            _cartRepository = cartRepository;
        }
        public async Task<IncomeResponse> Income(TimeIntervals time)
        {
            var (currentStart, currentEnd) = GetDateRange(time, DateTime.UtcNow);
            var (previosStart, previosEnd) = GetDateRange(time, currentStart);

            
            var CurrentSuccessfullOrders = await _orderrepository
                             .Query()
                             .Where(x => (x.OrderStatus == OrderStatus.CashOnDelivery
                             || x.OrderStatus == OrderStatus.PaymentReceived
                             || x.OrderStatus == OrderStatus.PaidByCoupon
                             || x.OrderStatus == OrderStatus.Complete)
                             && x.CreatedOn >= currentStart && x.CreatedOn < currentEnd)
                             .ToListAsync();

            var PrevSuccessfullOrders = await _orderrepository
                             .Query()
                             .Where(x => (x.OrderStatus == OrderStatus.CashOnDelivery
                             || x.OrderStatus == OrderStatus.PaymentReceived
                             || x.OrderStatus == OrderStatus.PaidByCoupon
                             || x.OrderStatus == OrderStatus.Complete)
                             && x.CreatedOn >= previosStart && x.CreatedOn < previosEnd)
                             .ToListAsync();

            var CurrentIncome = CurrentSuccessfullOrders
                .Sum(x => x.OrderItems.Sum(y => y.Product.Price * y.Quantity)
                - x.OrderItems.Sum(y => y.Product.Cost * y.Quantity)
                - x.DiscountAmount
                );

            var PrevIncome = PrevSuccessfullOrders
             .Sum(x => x.OrderItems.Sum(y => y.Product.Price * y.Quantity)
             - x.OrderItems.Sum(y => y.Product.Cost * y.Quantity)
             - x.DiscountAmount
             );
            double trend = ((double)(CurrentIncome - PrevIncome) / (double)PrevIncome) * 100;

            return new IncomeResponse
            {
                response = new Dictionary<string, IncomeVM>
                {
                    ["Income"] = new IncomeVM
                    {
                        trend = trend,
                        value = (double)CurrentIncome,
                        isPositive = trend > 0
                    }

                }
            };
        }

        public async Task<IncomeResponse> ConversionRate(TimeIntervals time)
        {
            var (currentStart, currentEnd) = GetDateRange(time, DateTime.UtcNow);
            var (previosStart, previosEnd) = GetDateRange(time, currentStart);
           
            var CurrentSuccessfullOrders = await _orderrepository
                             .Query()
                             .Where(x => (x.OrderStatus == OrderStatus.CashOnDelivery
                             || x.OrderStatus == OrderStatus.PaymentReceived
                             || x.OrderStatus == OrderStatus.PaidByCoupon
                             || x.OrderStatus == OrderStatus.Complete)
                             && x.CreatedOn >= currentStart && x.CreatedOn < currentEnd)
                             .ToListAsync();

            var PrevSuccessfullOrders = await _orderrepository
                             .Query()
                             .Where(x => (x.OrderStatus == OrderStatus.CashOnDelivery
                             || x.OrderStatus == OrderStatus.PaymentReceived
                             || x.OrderStatus == OrderStatus.PaidByCoupon
                             || x.OrderStatus == OrderStatus.Complete)
                             && x.CreatedOn >= previosStart && x.CreatedOn < previosEnd)
                             .ToListAsync();

            var CurrentCustomer = CurrentSuccessfullOrders.Select(x => x.CustomerId).Distinct().ToList();
            var PrevCustomer = PrevSuccessfullOrders.Select(x => x.CustomerId).Distinct().ToList();

            var currentUsers=await _usersrepository.Query().Where(x=>x.CreationDate>=currentStart&&x.CreationDate<currentEnd).Select(x=>x.Id).ToListAsync();
            var PreviosUsers = await _usersrepository.Query().Where(x => x.CreationDate >= previosStart&&x.CreationDate<previosEnd).Select(x => x.Id).ToListAsync();

            var currentdiffrence = currentUsers.Except(CurrentCustomer).Count();
            var prevdiffrence = PreviosUsers.Except(PrevCustomer).Count();


            double trend = ((double)(currentdiffrence - prevdiffrence) / (double)prevdiffrence) * 100;

            return new IncomeResponse
            {
                response = new Dictionary<string, IncomeVM>
                {
                    ["ConversionRate"] = new IncomeVM
                    {
                        trend = trend,
                        value = (double)currentdiffrence,
                        isPositive = trend > 0
                    }

                }
            };
        }

        public async Task<IncomeResponse> TotalOrders(TimeIntervals time)
        {
            var (currentStart, currentEnd) = GetDateRange(time, DateTime.UtcNow);
            var (previosStart, previosEnd) = GetDateRange(time, currentStart);
           
            var CurrentSuccessfullOrders = await _orderrepository
                             .Query()
                             .Where(x => (x.OrderStatus == OrderStatus.CashOnDelivery
                             || x.OrderStatus == OrderStatus.PaymentReceived
                             || x.OrderStatus == OrderStatus.PaidByCoupon
                             || x.OrderStatus == OrderStatus.Complete)
                             && x.CreatedOn >= currentStart && x.CreatedOn < currentEnd)
                             .ToListAsync();

            var PrevSuccessfullOrders = await _orderrepository
                             .Query()
                             .Where(x => (x.OrderStatus == OrderStatus.CashOnDelivery
                             || x.OrderStatus == OrderStatus.PaymentReceived
                             || x.OrderStatus == OrderStatus.PaidByCoupon
                             || x.OrderStatus == OrderStatus.Complete)
                             && x.CreatedOn >= previosStart && x.CreatedOn < previosEnd)
                             .ToListAsync();

            var CurrentPurches = CurrentSuccessfullOrders.Count();


            var PrevPurches = PrevSuccessfullOrders.Count();
            
            double trend = ((double)(CurrentPurches - PrevPurches) / (double)PrevPurches) * 100;

            return new IncomeResponse
            {
                response = new Dictionary<string, IncomeVM>
                {
                    ["Total Orders"] = new IncomeVM
                    {
                        trend = trend,
                        value = (double)CurrentPurches,
                        isPositive = trend > 0
                    }

                }
            };
        }

        public async Task<IncomeResponse> AbondomentRate(TimeIntervals time)
        {
            var (currentStart, currentEnd) = GetDateRange(time, DateTime.UtcNow);
            var (previosStart, previosEnd) = GetDateRange(time, currentStart);

            var CurrentcartsInPeriod = await _cartRepository.Query().Where(x => x.CreatedOn >= currentStart&&x.CreatedOn<currentEnd).ToListAsync();
            var PreviodcartsInPeriod = await _cartRepository.Query().Where(x => x.CreatedOn >= previosStart&&x.CreatedOn<previosEnd).ToListAsync();

            var CurrentSuccessfullOrders = await _orderrepository
               .Query()
               .Where(x => (x.OrderStatus == OrderStatus.CashOnDelivery
               || x.OrderStatus == OrderStatus.PaymentReceived
               || x.OrderStatus == OrderStatus.PaidByCoupon
               || x.OrderStatus == OrderStatus.Complete)
               && x.CreatedOn >= currentStart
               && x.CreatedOn<currentEnd)
               .ToListAsync();

            var PreviosSuccessfullOrders = await _orderrepository
             .Query()
             .Where(x => (x.OrderStatus == OrderStatus.CashOnDelivery
             || x.OrderStatus == OrderStatus.PaymentReceived
             || x.OrderStatus == OrderStatus.PaidByCoupon
             || x.OrderStatus == OrderStatus.Complete)
             && x.CreatedOn >= previosStart
             && x.CreatedOn < previosEnd)
             .ToListAsync();
            //Filter through carts that haven't been purchased within the selected period


            var CurrentAbandoned_CartsTotal = ((CurrentcartsInPeriod.Count()) - (CurrentSuccessfullOrders.Count()) / CurrentcartsInPeriod.Count()) * 100;
            var PrevAbandoned_CartsTotal = ((PreviodcartsInPeriod.Count()) - (PreviosSuccessfullOrders.Count()) / PreviodcartsInPeriod.Count()) * 100;

            var trend = ((double)(CurrentAbandoned_CartsTotal - PrevAbandoned_CartsTotal) / (double)PrevAbandoned_CartsTotal) * 100;
            return new IncomeResponse
            {
                response = new Dictionary<string, IncomeVM>
                {
                    ["Abandonment Rate"] = new IncomeVM
                    {
                        trend = trend,
                        value = (double)CurrentAbandoned_CartsTotal,
                        isPositive = trend > 0
                    }

                }
            };

        }
        private (DateTime start, DateTime end) GetDateRange(TimeIntervals period, DateTime referenceDate)
        {
            return period switch
            {
                TimeIntervals.Weekly => (referenceDate.AddDays(-7), referenceDate),
                TimeIntervals.Monthly => (referenceDate.AddMonths(-1), referenceDate),
                TimeIntervals.Yearly => (referenceDate.AddYears(-1), referenceDate),
                _ => throw new ArgumentOutOfRangeException(nameof(period), period, null)
            };
        }


    }
}
