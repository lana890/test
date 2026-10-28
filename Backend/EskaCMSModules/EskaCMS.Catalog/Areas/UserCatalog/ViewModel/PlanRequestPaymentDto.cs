using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.UserCatalog.Dtos
{
    public class PlanRequestPaymentDto
    {
        public bool isOneTimeService { get; set; }
        public bool IsTrail { get; set; }

        public string PlanName { get; set; }
        public decimal zeroDecimalOrderAmount { get; set; }
    }

    public class EmailViewModel
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
