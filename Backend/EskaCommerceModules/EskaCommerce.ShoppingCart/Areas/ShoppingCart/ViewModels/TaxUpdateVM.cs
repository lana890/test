using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels
{
    public class TaxUpdateVM
    {
        public bool IsTaxIncluded { get; set; }

        public string CountryId { get; set; }

        public CartTypes CartType { get; set; }

        public long? StateOrProvinceId { get; set; }

        public string? ZipCode { get; set; }

    }
}
