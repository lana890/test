using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.HOC.ManageStockQuentity.Models
{
    public class StockViewModel
    {
        public long ProductId { get; set; }
        public int Quantity { get; set; }

        public bool StockIsEnabled { get; set; }
    }
}
