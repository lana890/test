using EskaCommerce.Orders.Areas.Orders.ViewModels;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Orders.Events
{
    public class OrderDetailGot : INotification
    {
        public OrderDetailVm OrderDetailVm { get; set; }
    }
}
