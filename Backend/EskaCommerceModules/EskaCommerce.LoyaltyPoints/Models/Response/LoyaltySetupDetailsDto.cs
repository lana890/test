using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.LoyaltyPoints.Enums.LoyaltyPointsEnums;

namespace EskaCommerce.LoyaltyPoints.Models.Response
{
    public class LoyaltySetupDetailsDto
    {
        public long Id { get; set; }
        public decimal FromPrice { get; set; }
        public decimal ToPrice { get; set; }
        public decimal PointPerUnit { get; set; }
        public decimal PointAmount { get; set; }
        public string Status { get; set; }
        public ELoyalitySource LoyalitySource { get; set; }
        public string LoyalitySource_Converted { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public string ModifiedBy { get; set; }
        public string CreatedBy { get; set; }
        public string ProductName { get; set; }
        public long? ProductId { get; set; }
        public List<LoyaltyHistoryDto> LoyaltySetupHistory { get; set; }
    }
}
