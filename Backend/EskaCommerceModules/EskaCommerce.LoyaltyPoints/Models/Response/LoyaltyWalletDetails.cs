using EskaCMS.Entities.Entity.LoyaltyPoints;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.ShoppingCart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.LoyaltyPoints.Enums.LoyaltyPointsEnums;

namespace EskaCommerce.LoyaltyPoints.Models.Response
{
    public class LoyaltyWalletDetails
    {
        public decimal PointsAmount { get; set; }
        public string  TotalAmount { get; set; }
        public string  Status { get; set; }


    }
     
    public class LoyaltyTransaction
    {
        public decimal PointsAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string TotalValue { get; set; }
        public EWalletOperationType OperationType { get; set; }
       
        public long? OrderId { get; set; }
        public ELoyalitySource? LoyalitySource { get; set; }

        public DateTimeOffset CreationDate { get; set; }

        public string EncryptedOrderId { get; set; }

        public string Note { get; set; }
    }
}
