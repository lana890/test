using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.options
{
    

    public class ChangeAvailableOptionOrderVM
    {
        public long OptionId { get; set; }
        public List<OrderingOptionValue> OrderingOptionValue { get; set; }
    }


    public class  OrderingOptionValue
    {

        public long ValueId { get; set; }
        public int DisplayOrder { get; set; }

    }



}
