using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels
{
	public class GetCartByCartIdVM
	{
		public long cartId { get; set; }
		public string ReferenceId { get; set; }
		public CartTypes cartType { get; set; }
	} 



}
