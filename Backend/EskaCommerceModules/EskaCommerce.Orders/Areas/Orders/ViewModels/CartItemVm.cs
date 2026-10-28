using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class CartItemVm
    {
        public int Quantity;

        public decimal Price;

        public long? TaxClassId;
    }
}
