using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.LoyaltyPoints.Models.Request
{
    public class AddPointsDto
    {
        [Required]
        public string Note { get; set; }
        [Required]
        public decimal PointsAmount { get; set; }
    }
}
