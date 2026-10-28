using EskaCommerce.Orders.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// Starts the server flows of an order the moment it becomes paid. It only creates and queues
    /// runs (a few quick queries); the calls themselves run on the worker. It never throws into the
    /// order code: if it fails, the worker's sweep creates the runs a little later.
    /// </summary>
    public sealed class OrderPaidServerFlowHandler : INotificationHandler<OrderChanged>
    {
        private readonly ServerFlowTriggers _triggers;
        private readonly ILogger<OrderPaidServerFlowHandler> _logger;

        public OrderPaidServerFlowHandler(ServerFlowTriggers triggers, ILogger<OrderPaidServerFlowHandler> logger)
        {
            _triggers = triggers;
            _logger = logger;
        }

        public async Task Handle(OrderChanged notification, CancellationToken cancellationToken)
        {
            if (notification.OldStatus == notification.NewStatus || !ServerFlowTriggers.PaidStatuses.Contains(notification.NewStatus))
                return;

            try
            {
                await _triggers.OrderPaidAsync(notification.OrderId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server flows: could not create the runs for order {OrderId}; the sweep will retry.", notification.OrderId);
            }
        }
    }
}
