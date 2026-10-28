using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.LoyaltyPoints.Models.Response
{
    public class LoyaltyTransactionsDto
    {
        public decimal PointsAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string OperationType { get; set; }
    }
}
