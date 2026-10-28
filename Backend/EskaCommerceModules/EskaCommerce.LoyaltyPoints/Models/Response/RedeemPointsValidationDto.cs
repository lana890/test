using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.LoyaltyPoints.Models.Response
{
    public class RedeemPointsValidationDto
    {
        public bool Succeeded { get; set; }
        public decimal TotalAmmountAfterDiscount { get; set; }

        public decimal RedeemDiscount { get; set; }

        public string ErrorMessage { get; set; }

    }
}
