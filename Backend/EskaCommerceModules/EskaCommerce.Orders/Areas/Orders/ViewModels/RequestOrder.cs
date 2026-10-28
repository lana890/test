using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class RequestOrder
    {

        public string userName { get; set; }
        public string OrderTime { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<Products> products { get; set; }
    }
    public class Products
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
