using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels
{
    public class UpdateCartInfoVM
    {
        public string AdditionalInfo { get; set; }
		public string ReferenceId { get; set; }
		public CartTypes CartType { get; set; }
        public long? ProductId { get; set; }
        public int Quantity { get; set; }
		public long? CartId { get; set; }
	}
}
