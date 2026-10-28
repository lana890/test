using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.UserCatalog.Dtos
{
   public class UserPlanProductsVM
    {
        public long  UserPlanId { get; set; }
        public List<Products> Products { get; set; }
    }

    public class Products
    {
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal ProductPrice { get; set; }
    }
}
