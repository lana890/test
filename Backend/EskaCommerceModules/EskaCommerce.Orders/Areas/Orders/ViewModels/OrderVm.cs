using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class GridOutputVM<T>
    {
        public List<T> Items { get; set; }
        public int TotalRecord { get; set; }
    }
    public class OrderListOutputVM
    {
        public DateTimeOffset OrderDate { get; set; }
        public EskaCMS.Entities.Entity.OrderStatus OrderStatus { get; set; }
        public long OrderId { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerCountry { get; set; }
        public string CustomerCity { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerPhoneNumber { get; set; }
    }
    public class SearchTableParam<T>
    {
        public Pagination Pagination { get; set; }

        public T Search { get; set; }

    }

   
    public class OrderListSearchVM
    {
        public DateTimeOffset? OrderDateFrom { get; set; }
        public DateTimeOffset? OrderDateTo { get; set; }
        public EskaCMS.Entities.Entity.OrderStatus? OrderStatus { get; set; }
        public string StringSearch { get; set; }
    }

    public class OrderIdVM
    {
         
        public string orderId { get; set; }
       
    }

  

    public class AdjustWarehouseOrderVM
    {
        public long WarehouseId { get; set; }
        public long OrderId { get; set; }

    }
}
