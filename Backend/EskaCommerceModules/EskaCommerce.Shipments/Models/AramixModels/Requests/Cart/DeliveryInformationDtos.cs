using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Shipments.Models.AramixModels.Requests.Cart
{
    public class DeliveryInformationDtos
    {
        public DeliveryInformationDtos()
        {
            NewAddressForm = new AddressFormDtos();
        }

        public IList<ShippingAddresDtos> ExistingShippingAddresses { get; set; } =
            new List<ShippingAddresDtos>();

        public long ShippingAddressId { get; set; }

        public long BillingAddressId { get; set; }

        public string ShippingMethod { get; set; }

        public AddressFormDtos NewAddressForm { get; set; }

        public bool UseShippingAddressAsBillingAddress { get; set; }

        public AddressFormDtos NewBillingAddressForm { get; set; }
        public List<AdditionalSettingsDtos> AdditionalInfo { get; set; }
        public string OrderNote { get; set; }
        public string full_number { get; set; }
        public string full_number_billing { get; set; }
        public CartTypes CartTypes { get; set; }

    }

}
