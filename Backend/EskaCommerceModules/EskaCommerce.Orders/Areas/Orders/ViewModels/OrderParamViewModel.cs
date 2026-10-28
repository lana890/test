using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class OrderParamViewModel
    {
        public List<OrderHistoryListItem> Items { get; set; } = new List<OrderHistoryListItem>();
        public long TotalItem { get; set; }
    }
}
