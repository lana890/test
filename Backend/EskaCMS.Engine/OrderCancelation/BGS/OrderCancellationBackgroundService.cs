using EskaCMS.Core.Services;
using EskaCMS.Engine.BGS;
using EskaCMS.Engine.OrderCancelation.Scripts;
using EskaCMS.Engine.Services.Interfaces;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Engine.OrderCancelation.BGS
{
    public class OrderCancellationBackgroundService : BaseBackgroundService
    {
        private long SystemUserId = 2;

        private readonly ILogger<OrderCancellationBackgroundService> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly double CancelAfterInMinutes;
        public OrderCancellationBackgroundService(IServiceProvider serviceProvider
            , ILogger<OrderCancellationBackgroundService> logger
            , IConfiguration configuration)
            : base(nameof(OrderCancellationBackgroundService), logger, serviceProvider, configuration)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            _configuration = configuration;
            CancelAfterInMinutes = double.Parse(configuration.GetSection("BGSs:OrderCancelation:CancelAfter")?.Value);

        }

        public override async Task<bool> Execute()
        {
            try
            {

                logger.LogInformation("OrderCancellationBackgroundService is working.");

                using var scope = serviceProvider.CreateScope();
                await CancelFailedPaymentOrders(
                    scope.ServiceProvider.GetRequiredService<IRepository<Order>>(),
                    scope.ServiceProvider.GetRequiredService<IRepository<OrderItem>>(),
                    scope.ServiceProvider.GetRequiredService<IRepository<ProductLink>>(),
                    scope.ServiceProvider.GetRequiredService<IRepository<ProductSerial>>(),
                    scope.ServiceProvider.GetRequiredService<IRepository<OrderHistory>>(),
                    scope.ServiceProvider.GetRequiredService<IRepository<DCMSLog>>(),
                    scope.ServiceProvider.GetRequiredService<IRepository<ProductSerialHistory>>(),
                    scope.ServiceProvider.GetRequiredService<IRepository<Payment>>());

            }
            catch (Exception e)
            {
                using var scope = serviceProvider.CreateScope();
                var dcmsLogService = scope.ServiceProvider.GetRequiredService<IdcmsLogService>();
                await dcmsLogService.AddError("Order Cancellation BackgroundService", "", e.Message, "", null);

            }
            return true;
        }
        public async Task CancelFailedPaymentOrders(
            IRepository<Order> orderRepository,
            IRepository<OrderItem> ordeItemRepository,
            IRepository<ProductLink> productLinkRepository,
            IRepository<ProductSerial> productSerialRepository,
            IRepository<OrderHistory> orderHistoryRepository,
            IRepository<DCMSLog> dcmsLogRepository,
            IRepository<ProductSerialHistory> productSerialHistoryRepository,
            IRepository<Payment> paymentRepository)
        {
            try
            {

                var durationToCancel = DateTimeOffset.Now.ToUniversalTime();
                var failedPaymentOrders = await orderRepository.Query().Where(x =>
                    (x.OrderStatus == OrderStatus.PendingPayment || x.OrderStatus == OrderStatus.PaymentFailed)
                    && x.CreatedOn.AddMinutes(CancelAfterInMinutes) < durationToCancel
                    // Money was taken (or is held for review): never cancel it automatically. A person
                    // refunds or completes it.
                    && !paymentRepository.Query().Any(p => p.OrderId == x.Id && (p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.OnHold))).ToListAsync();

                foreach (var order in failedPaymentOrders)
                {
                    try
                    {
                        // Read again right before cancelling: a payment can succeed while the
                        // earlier orders of this list are being cancelled.
                        var stillUnpaid = await orderRepository.Query().AsNoTracking()
                            .AnyAsync(x => x.Id == order.Id
                                && (x.OrderStatus == OrderStatus.PendingPayment || x.OrderStatus == OrderStatus.PaymentFailed)
                                && !paymentRepository.Query().Any(p => p.OrderId == x.Id && (p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.OnHold)));
                        if (!stillUnpaid)
                            continue;

                        var List = await CancelOrder(order, ordeItemRepository, productLinkRepository, productSerialRepository);
                        var orderStatusChanged = new OrderHistory
                        {
                            OrderId = order.Id,
                            OldStatus = OrderStatus.PendingPayment,
                            NewStatus = OrderStatus.Canceled,
                            Order = order,
                            Note = "System canceled",
                            CreatedById = SystemUserId
                        };
                        await orderRepository.SaveChangesAsync();
                        await ChangeOrderHistory(orderStatusChanged, orderHistoryRepository);
                        await orderRepository.SaveChangesAsync();

                        if (List.Count > 0)
                        {
                            await UpdateSerialProduct(List, order.Id, productSerialHistoryRepository);

                        }

                    }
                    catch (Exception ex)
                    {
                        var error = new DCMSLog
                        {
                            Scope = "OrderCancellationBackgroundService",
                            SiteId = order.SiteId,
                            Date = DateTimeOffset.Now,
                            AdditionInfo = ex.StackTrace,
                            Message = ex.Message,
                            Exception = ex.Message,
                            Module = "OrderCancellationBackgroundService",
                            Channel = EPlatformSource.Unknown,
                            UserId = order.CustomerId
                        };
                        dcmsLogRepository.Add(error);
                        await dcmsLogRepository.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        private async Task ChangeOrderHistory(OrderHistory orderStatusChanged, IRepository<OrderHistory> orderHistoryRepository)
        {
            try
            {
                orderHistoryRepository.Add(orderStatusChanged);
                await orderHistoryRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<List<long>> CancelOrder(
            Order order,
            IRepository<OrderItem> ordeItemRepository,
            IRepository<ProductLink> productLinkRepository,
            IRepository<ProductSerial> productSerialRepository)
        {
            try
            {

                order.OrderStatus = OrderStatus.Canceled;
                order.LatestUpdatedOn = DateTimeOffset.Now;
                List<long> ProductSerilaId = new List<long>();
                var orderItems = ordeItemRepository.Query().Include(x => x.Order).Include(x => x.Product).Where(x => x.Order.Id == order.Id).ToList();
                foreach (var item in orderItems)
                {
                    var OriginalProductId = item.ProductId;

                    var IsProductSerial = item.Product.IsProductSerial;

                    var OriginalProduct = await productLinkRepository.Query().Include(x => x.Product).Where(x => x.LinkedProductId == item.ProductId).FirstOrDefaultAsync();

                    if (OriginalProduct != null)
                    {

                        OriginalProductId = OriginalProduct.ProductId;

                        IsProductSerial = OriginalProduct.Product.IsProductSerial;
                    }

                    if (IsProductSerial)
                    {
                        var ProductSerialForOrder = await productSerialRepository.Query().Where(ps => ps.ProductId == OriginalProductId && ps.OrderId == order.Id).ToListAsync();

                        if (ProductSerialForOrder.Count > 0)
                        {
                            await productSerialRepository.SQLQuery(MakeProductSerialAvaliable.MakeSerialsAvaliable, order.Id);
                            ProductSerilaId = ProductSerialForOrder.Select(x => x.Id).ToList();
                        }
                        await ordeItemRepository.SaveChangesAsync();

                    }

                }
                return ProductSerilaId;


            }
            catch (Exception e)
            {

                throw;
            }

        }

        public async Task<bool> UpdateSerialProduct(
            List<long> SerialIds,
            long orderId,
            IRepository<ProductSerialHistory> productSerialHistoryRepository)
        {
            try
            {
                foreach (var SerialId in SerialIds)
                {


                    productSerialHistoryRepository.Add(new EskaCMS.Entities.Entity.Catalog.ProductSerialHistory()
                    {
                        OldStatus = EProductSerialStatus.Taken,
                        CreatedById = 1,
                        CreationDate = DateTimeOffset.Now,
                        NewStatus = EProductSerialStatus.Available,
                        ProductSerialId = SerialId,
                        OrderId = null,
                        Note = "Cancellation Order with OrderId= " + orderId.ToString()
                    });

                    await productSerialHistoryRepository.SaveChangesAsync();

                }



                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
