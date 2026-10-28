using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class OrderHistoryProductVm
    {
        public long ProductId { get; set; }

        public string ProductName { get; set; }

        public List<OrderItemsProductOptionVM> ProductOptions { get; set; } = new List<OrderItemsProductOptionVM>();

        public string ProductOptionString
        {
            get { return string.Join(",", ProductOptions.Select(x=>x.Value).ToList()); }
        }

        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public string ThumbnailImage { get; set; }
        public string Currency { get; set; }

        public List<CategoryValue> Category { get; set; } = new List<CategoryValue>();
    }
    public class OrderItemsProductOptionVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

    }

    public class CategoryValue
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

    }
}
