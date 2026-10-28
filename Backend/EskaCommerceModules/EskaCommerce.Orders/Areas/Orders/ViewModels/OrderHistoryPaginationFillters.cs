using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class OrderHistoryPaginationFillters
    {
        public List<OrderStatus>? OrderStatuses { get; set; }
    }
}
