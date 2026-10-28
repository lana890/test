using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels
{
    public class AddToCartModel
    {
        public long ProductId { get; set; }
        public long? PlanVersionId { get; set; }

        public string VariationName { get; set; }

        public int Quantity { get; set; }

        public string AdditionalDetails { get; set; }
        public CartTypes CartType { get; set; }
		public string ReferenceId { get; set; }


	}

  
}
