using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Discount;
using EskaCMS.Entities.Entity.Pricing;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Discount.Models
{
    public class DiscountVM
    {

        public string Name { get; set; }

        public string Description { get; set; }
        public DiscountTypes DiscountType { get; set; }
        public string DiscountTypeDesc { get; set; }
        public decimal DiscountValue { get; set; }
        public IList<EskaCMS.Entities.Entity.Discount.DiscountTiers> DiscountTiers { get; set; }
        public long SiteId { get; set; }
        public long Id { get; set; }
        public bool IsCustomerGroup { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? ExpiredTime { get; set; }
        public IList<CustomerGroupDiscountViewModel> CustomerGroups { get; set; } = new List<CustomerGroupDiscountViewModel>();
        public IList<ProductDiscountViewModel> Products { get; set; } = new List<ProductDiscountViewModel>();
        public bool IsProduct { get; set; }

    }

    public class CustomerGroupDiscountViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
    public class DiscountCustomerGroupViewModel
    {


        public IList<long> CustomerGroupIds { get; set; } = new List<long>();

        public long DiscountId { get; set; }

    }

    public class ProductDiscountViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public bool IsPublished { get; set; }

    }

    public class GetDiscountCustomerGroupViewModel
    {


        public IList<DetailsDiscountCustomerGroupViewModel> CustomerGroup { get; set; } = new List<DetailsDiscountCustomerGroupViewModel>();

        public long DiscountId { get; set; }

    }

    public class DetailsDiscountCustomerGroupViewModel
    {
        public string Name { get; set; }
        public long Id { get; set; }


    }

}
