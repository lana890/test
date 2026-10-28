using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Shipments
{
    public class Pickup:EntityBase
    {

            public string PickupId { get; set; }
            public string GUID { get; set; }
            public string Reference1 { get; set; }
            public string Reference2 { get; set; }

            public DateTimeOffset CreatedOn { get; set; }

            public long CreatedById { get; set; }

            public User CreatedBy { get; set; }

            public IList<Shipment> Shipments { get; set; }

    }
}
