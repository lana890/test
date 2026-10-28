using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class TaxAndShippingPriceRequestVm
    {
        public string SelectedShippingMethodName { get; set; }

        public ShippingAddressVm NewShippingAddress { get; set; }

        public ShippingAddressVm NewBillingAddress { get; set; }

        public long ExistingShippingAddressId { get; set; }
        public CartTypes CartType { get; set; }
    }
}
