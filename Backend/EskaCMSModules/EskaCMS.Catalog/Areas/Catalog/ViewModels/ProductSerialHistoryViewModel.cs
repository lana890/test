using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductSerialHistoryViewModel
    {
        public long ProductSerialId { get; set; }
        public long? OrderId { get; set; }
        public string Code { get; set; }
        public string OldStatus { get; set; } 
        public string NewStatus { get; set; }
        public string Note { get; set; }
    }
}
