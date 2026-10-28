using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.LoyaltyPoints.Models.Response
{
    public class LoyaltyHistoryDto
    {
        public decimal FromPrice { get; set; }
        public decimal ToPrice { get; set; }
        public decimal PointPerUnit { get; set; }
        public decimal PointAmount { get; set; }
        public string Status { get; set; }
        public string LoyalitySource { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string CreatedBy { get; set; }
        public long? ProductId { get; set; }
        public string ProductName { get; set; }

    }
}
