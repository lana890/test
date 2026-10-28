using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.ModelVM.Request
{
    public class ProductViewsRequest
    {
        public int ProductId { get; set; }
        public TimeRequest timeRequest { get; set; }

    }
}
