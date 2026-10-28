using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.EskaBilling.Models.response
{
    public class SearchSimsViewModel
    {
        public long? ProductId { get; set; }
        public string CustomerName { get; set; }
        public long? CustomerId { get; set; }

        public string City { get; set; }
        public EIOTSIMStatus? Status { get; set; }
    }
}
