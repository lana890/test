#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Payments.Models.PayFort
{
    public class PayFortWebRequest
    {
       public CartTypes CartType {  get; set; }
        public string? CP_URL { get; set; }
        public int Type { get; set; }
		public long? CartId { get; set; }
	}
}
