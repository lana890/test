using EskaCommerce.Orders.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.ModelVM.Request
{
    public class CustomerActions
    {
      public CustomerActionEnum action {  get; set; }
      public List<long>? CustomerIds { get; set; }
      public object Payload { get; set; }

    }
    
}
