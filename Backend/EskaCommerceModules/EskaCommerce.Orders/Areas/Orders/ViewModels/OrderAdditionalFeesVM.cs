using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class OrderAdditionalFeesVM
    {
        public string ProductName { get; set; }
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string FeeName { get; set; }
        public decimal Amount { get; set; }

        public string Note { get; set; }
        public bool Refundable { get; set; }
        public ProcessReversal Status { get; set; }
        public IList<OrderAdditionalFeesHistoryVM> History { get; set; }
    }

    public class UpdateOrderAdditionalFeesVM
    {
        public long? OrderId { get; set; }
        public long? OrderAdditionalFeesId { get; set; }
        public  bool CheckAllFees { get; set; }
        public  string Note { get; set; }

    }
    public class OrderAdditionalFeesHistoryVM
    {


        public ProcessReversal? OldStatus { get; set; }

        public ProcessReversal NewStatus { get; set; }

        public string Note { get; set; }

    }


}
