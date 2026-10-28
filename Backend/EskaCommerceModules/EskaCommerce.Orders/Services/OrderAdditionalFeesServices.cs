using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.LoyaltyPoints.Services;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.Payments.Helper;
using EskaCommerce.Payments.Services.Interface;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Orders.Services
{
    public class OrderAdditionalFeesServices : IOrderAdditionalFeesServices
    {
        private readonly IRepository<OrderAdditionalFees> orderAdditionalFeesRepository;
        private readonly IRepository<OrderAdditionalFeesHistory> orderAdditionalFeesHistoryRepository;
        private readonly IRepository<Order> orderRepository;
        private readonly ILoyaltyWalletService loyaltyWalletService;
        private readonly IWorkContext workContext;
        private readonly IPaymentServices paymentServices;

        public OrderAdditionalFeesServices(IRepository<OrderAdditionalFees> OrderAdditionalFeesRepository,
            IRepository<OrderAdditionalFeesHistory> OrderAdditionalFeesHistoryRepository,
            IRepository<Order> orderRepository,
            ILoyaltyWalletService loyaltyWalletService,
            IWorkContext workContext, IPaymentServices paymentServices)
        {
            orderAdditionalFeesRepository = OrderAdditionalFeesRepository;
            orderAdditionalFeesHistoryRepository = OrderAdditionalFeesHistoryRepository;
            this.orderRepository = orderRepository;
            this.loyaltyWalletService = loyaltyWalletService;
            this.workContext = workContext;
            this.paymentServices = paymentServices;
        }

        public async Task<bool> AddOrderAdditionalFees(List<OrderAdditionalFees> orderAdditionalFees, long OrderId)
        {
            try
            {
                foreach (var orderAdditionalFeesPerOne in orderAdditionalFees)
                {
                    orderAdditionalFeesPerOne.OrderId = OrderId;

                    orderAdditionalFeesRepository.Add(orderAdditionalFeesPerOne);
                    await orderAdditionalFeesRepository.SaveChangesAsync();

                    var orderAdditionalFeesHistory = new OrderAdditionalFeesHistory
                    {
                        CreationDate = DateTime.Now,
                        CreatedById = await workContext.GetCurrentUserId(),
                        OldStatus = null,
                        NewStatus = ProcessReversal.Paid,
                        Note = orderAdditionalFeesPerOne.Note,
                        OrderAdditionalFeesId = orderAdditionalFeesPerOne.Id,

                    };

                    orderAdditionalFeesHistoryRepository.Add(orderAdditionalFeesHistory);
                    await orderAdditionalFeesHistoryRepository.SaveChangesAsync();
                }

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> UpdateFeesToRefaund(UpdateOrderAdditionalFeesVM model)
        {
            try
            {


                var Order = new Order();
                decimal Amount = 0;
                var Query = orderAdditionalFeesRepository
                                                                        .Query()
                                                                        .Include(x => x.Fees)
                                                                        .Include(x => x.OrderAdditionalFeesHistories)
                                                                        .Include(x => x.Order);
                var OrderAdditionalFees = new List<OrderAdditionalFees>();
                if (model.CheckAllFees)
                {
                    OrderAdditionalFees = await Query.Where(x => x.OrderId == model.OrderId && x.Status == ProcessReversal.Paid)
                                                                        .ToListAsync();


                }
                else
                {
                    OrderAdditionalFees = await Query.Where(x => x.Id == model.OrderAdditionalFeesId && x.Status == ProcessReversal.Paid)
                                                                           .ToListAsync();

                }
                if (OrderAdditionalFees.Count() > 0)
                {
                    Order = await orderRepository.Query().Where(x => x.Id == OrderAdditionalFees.Select(c => c.OrderId).FirstOrDefault()).FirstOrDefaultAsync();

                }
                foreach (var OrderAdditionalFeesPerOne in OrderAdditionalFees)
                {
                    if (OrderAdditionalFeesPerOne.Fees.Refundable)
                    {

                        OrderAdditionalFeesPerOne.Status = ProcessReversal.Refaund;
                        OrderAdditionalFeesPerOne.Note = model.Note;

                        if (OrderAdditionalFeesPerOne.OrderAdditionalFeesHistories != null)
                        {
                            var LastOrderAdditionalFeesHistories = OrderAdditionalFeesPerOne.OrderAdditionalFeesHistories.OrderByDescending(x => x.Id).Select(x => x.NewStatus).FirstOrDefault();

                            var NewOrderAdditionalFeesHistory = new OrderAdditionalFeesHistory
                            {

                                CreationDate = DateTime.Now,
                                CreatedById = await workContext.GetCurrentUserId(),
                                NewStatus = ProcessReversal.Refaund,
                                OldStatus = LastOrderAdditionalFeesHistories,
                                OrderAdditionalFeesId = OrderAdditionalFeesPerOne.Id,
                                Note = model.Note,

                            };
                            orderAdditionalFeesHistoryRepository.Add(NewOrderAdditionalFeesHistory);
                            await orderAdditionalFeesHistoryRepository.SaveChangesAsync();

                        }


                         Amount += OrderAdditionalFeesPerOne.Amount;
                        await paymentServices.CreatePayment(OrderAdditionalFeesPerOne.Order, OrderAdditionalFeesPerOne.OrderId.ToString(), PaymentHelper.RefundAmount, null, PaymentStatus.Succeeded, null, OrderAdditionalFeesPerOne.Amount);
                    }
                }
                if (Order.OrderTotal > 0)
                {
                    Order.OrderTotal = Order.OrderTotal - Amount;
                    if (Order.OrderTotal < 0)
                    { Order.OrderTotal = 0; }
                    await orderRepository.SaveChangesAsync();
                }
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
