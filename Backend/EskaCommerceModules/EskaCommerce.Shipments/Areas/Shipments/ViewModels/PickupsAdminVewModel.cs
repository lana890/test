using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Shipments.Areas.Shipments.ViewModels
{
    public class PickupsAdminVewModel
    {
        public long Id { get; set; }
        public string PickupId { get; set; }
        public string GUID { get; set; }
        public string Reference1 { get; set; }
        public string Reference2 { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public string UserName { get; set; }

        public User CreatedBy { get; set; }

        public IList<ShipmentAdminViewModel> Shipments { get; set; }=new List<ShipmentAdminViewModel>();
    }
}
