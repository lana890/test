using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Orders.ModelVM.Request
{
    public class CustomerFilteration
    {
        public string searchTerm { get; set; }

        public DateTime? dateFrom { get; set; }

        public DateTime? dateTo { get; set; }

        public EStatus? status { get; set; }

        public int page { get; set; }

        public int limit { get; set; }

        public string sortBy { get; set; }

        public ESortType sortOrder { get; set; }
    }
}
