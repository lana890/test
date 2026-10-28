using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using EskaCMS.Entities.Entity.Orders;

namespace EskaCommerce.Orders.Events
{
    public class OrderChangedCreateOrderHistoryHandler : INotificationHandler<OrderChanged>
    {
        private readonly IRepository<OrderHistory> _orderHistoryRepository;

        // Shared, not created per call: a fresh options instance discards System.Text.Json's per-type
        // metadata, which then has to be rebuilt for the whole (large) order graph. Same settings, same output.
        private static readonly JsonSerializerOptions SnapshotSerializerOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        public OrderChangedCreateOrderHistoryHandler(IRepository<OrderHistory> orderHistoryRepository)
        {
            _orderHistoryRepository = orderHistoryRepository;
        }

        public async Task Handle(OrderChanged notification, CancellationToken cancellationToken)
        {
            var orderHistory = new OrderHistory
            {
                OrderId = notification.OrderId,
                CreatedOn = DateTimeOffset.Now,
                CreatedById = notification.UserId,
                OldStatus = notification.OldStatus,
                NewStatus = notification.NewStatus,
                Note = notification.Note,
            };

            if (notification.Order != null)
            {
                orderHistory.OrderSnapshot = System.Text.Json.JsonSerializer.Serialize(notification.Order, SnapshotSerializerOptions);

            }

            _orderHistoryRepository.Add(orderHistory);
            await _orderHistoryRepository.SaveChangesAsync();
        }
    }

}
