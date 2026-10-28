using EskaCMS.Entities.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Orders;

namespace EskaCommerce.Orders.Events
{
    public class OrderChanged : INotification,IRequest
    {
        public long OrderId { get; set; }

        public Order Order { get; set; }

        public OrderStatus? OldStatus { get; set; }

        public OrderStatus NewStatus { get; set; }

        public long UserId { get; set; }

        public string Note { get; set; }
    }
}
