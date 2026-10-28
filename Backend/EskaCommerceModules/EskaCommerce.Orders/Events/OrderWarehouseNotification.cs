using EskaCMS.Entities.Entity.Orders;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Events
{
    public class OrderWarehouseNotification : INotification
    {

        public Order Order { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }


    }
}
