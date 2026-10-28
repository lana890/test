#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Payments.Models.PaymentPaypalExpress
{
    public class CreatePaymentDto
    {
        [Required]
        public CartTypes CartType { get; set; }
        [Required]
        public string ReturnUrl { get; set; }
        [Required]
        public string CancelUrl { get; set; }
    }
}
