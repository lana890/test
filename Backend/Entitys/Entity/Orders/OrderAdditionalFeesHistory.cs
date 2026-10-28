using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Orders
{
    public class OrderAdditionalFeesHistory : EntityBase
    {
        public long OrderAdditionalFeesId { get; set; }

        public OrderAdditionalFees OrderAdditionalFees { get; set; }

        public ProcessReversal? OldStatus { get; set; }

        public ProcessReversal NewStatus { get; set; }

        public string Note { get; set; }

        public DateTimeOffset CreationDate { get; set; }

        public long CreatedById { get; set; }

        public User CreatedBy { get; set; }
    }
}
