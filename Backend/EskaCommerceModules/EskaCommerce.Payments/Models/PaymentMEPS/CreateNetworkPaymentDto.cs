using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public class CreateNetworkPaymentDto
    {
        [Required]
        public CartTypes CartType { get; set; }
        [Required]
        public string RedirectUrl { get; set; }
       
    }
}
