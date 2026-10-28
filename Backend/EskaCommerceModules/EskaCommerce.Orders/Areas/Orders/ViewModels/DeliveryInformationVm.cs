using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class DeliveryInformationVm
    {
        public DeliveryInformationVm()
        {
            NewAddressForm = new AddressFormVm();
        }

        public IList<ShippingAddressVm>? ExistingShippingAddresses { get; set; } =
            new List<ShippingAddressVm>();

        public long ShippingAddressId { get; set; }

        public long BillingAddressId { get; set; }

        public string ShippingMethod { get; set; }

        public AddressFormVm? NewAddressForm { get; set; } =
            new AddressFormVm();

        public bool UseShippingAddressAsBillingAddress { get; set; }

        public AddressFormVm? NewBillingAddressForm { get; set; } =
            new AddressFormVm();
        public List<AdditionalSettingsVM>? AdditionalInfo { get; set; } = new List<AdditionalSettingsVM>();
        public string OrderNote { get; set; }
        public string full_number { get; set; }
        public string full_number_billing { get; set; }
        public CartTypes CartTypes { get; set; }

        public bool? IsPickuped { get; set; }
        public long? WarehouseId { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string CustomerName { get; set; }

		public long? CartId { get; set; }

	}

}
