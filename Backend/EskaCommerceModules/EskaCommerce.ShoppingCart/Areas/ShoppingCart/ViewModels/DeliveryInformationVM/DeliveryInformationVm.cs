using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels.DeliveryInformationVM
{
    public class DeliveryInformationVm
    {
        public DeliveryInformationVm()
        {
            NewAddressForm = new AddressFormVm();
        }

        public IList<ShippingAddressVm> ExistingShippingAddresses { get; set; } =
            new List<ShippingAddressVm>();

        public long ShippingAddressId { get; set; }

        public long BillingAddressId { get; set; }

        public string ShippingMethod { get; set; }

        public AddressFormVm NewAddressForm { get; set; }

        public bool UseShippingAddressAsBillingAddress { get; set; }

        public AddressFormVm NewBillingAddressForm { get; set; }
        public List<AdditionalSettingsVM> AdditionalInfo { get; set; }
        public string OrderNote { get; set; }
        public string full_number { get; set; }
        public string full_number_billing { get; set; }
        public CartTypes CartTypes { get; set; }

        public bool? IsPickuped { get; set; }
        public long? WarehouseId { get; set; }

    }
}
