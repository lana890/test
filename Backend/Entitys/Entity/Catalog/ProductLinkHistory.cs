using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductLinkHistory : EntityBase
    {

        public EHistoryAction Action { get; set; }
        public long ProductId { get; set; }

        public Product Product { get; set; }

        public long LinkedProductId { get; set; }

        public Product LinkedProduct { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public ItemLinkType LinkType { get; set; }

        public long CreatedById { get; set; }

        public User CreatedBy { get; set; }
    }
}
