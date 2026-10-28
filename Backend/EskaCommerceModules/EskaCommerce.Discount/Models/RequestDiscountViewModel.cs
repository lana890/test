using EskaCMS.Core.Areas.Core.Interfaces;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Discount.Models
{
    public class RequestDiscountViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
        public DiscountTypes DiscountType { get; set; }
        public string DiscountTypeDesc { get; set; }
        public decimal DiscountValue { get; set; }
        public IList<RequestDiscountTiersViewModel> DiscountTiers { get; set; }
        public bool IsCustomerGroup { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? ExpiredTime { get; set; }

        public IList<long> CustomerGroups { get; set; } = new List<long>();
        public IList<long> Products { get; set; } = new List<long>();
    }

    public class RequestDiscountTiersViewModel
    {

        public long DiscountId { get; set; }
        public decimal FromValue { get; set; }
        public decimal ToValue { get; set; }

        public DiscountTiersTypes DiscountTierType { get; set; }
        public decimal DiscountValue { get; set; }
    }
}



