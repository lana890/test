using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.LoyaltyPoints.Enums.LoyaltyPointsEnums;

namespace EskaCommerce.LoyaltyPoints.Models.Request
{
    public class LoyaltySetupDto
    {
        public decimal FromPrice { get; set; }
        public decimal ToPrice { get; set; }
        public decimal PointPerUnit { get; set; }
        public decimal PointAmount { get; set; }
        public ELoyalitySource LoyalitySource { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public long? ProductId { get; set; }

       
    }
}
