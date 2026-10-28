using EskaCMS.Entities.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Orders;

namespace EskaCommerce.Orders.Events
{
    public class OrderCreated : INotification
    {
        public long OrderId { get; set; }

        public Order Order { get; set; }

        public long UserId { get; set; }

        public string Note { get; set; }
        public OrderStatus OrderStatus { get; set; }
    }
}
