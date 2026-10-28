using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class GetOrderStatistcsVM
    {
        public long? UserId { get; set; }
        public long? WarehouseId { get; set; }

        [Required]
        public DateTimeOffset From { get; set; }
        [Required]
        public DateTimeOffset To { get; set; }
    }

    public class GetOrderStatistcsDto
    {
        public int OrderCount { get; set; }
        public decimal TotalOrders { get; set; }
        public DateTimeOffset Date { get; set; }
        public string  FormatedDate { get; set; }
    }

    public class DailyOrderStatistics
    {
        public int OrderCount { get; set; }
        public decimal TotalOrdersAmount { get; set; }

    }
}
