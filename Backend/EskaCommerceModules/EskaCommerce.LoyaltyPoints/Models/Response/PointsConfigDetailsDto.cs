using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.LoyaltyPoints.Models.Response
{
    public class PointsConfigDetailsDto
    {
        public decimal PointsValue { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset? ModificationDate { get; set; }
    }
}
