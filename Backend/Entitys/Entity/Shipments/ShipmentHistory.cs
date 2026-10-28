using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Shipments
{
    public class ShipmentHistory:EntityBase
    {
        public long ShipmentId { get; set; }

        public Shipment  Shipment { get; set; }

        public ShippmentStatus?OldStatus { get; set; }
     
        public ShippmentStatus NewStatus { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public long CreatedById { get; set; }

        public User CreatedBy { get; set; }
    }
}
