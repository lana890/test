using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.Entities.Entity
{
    public class ChangePlanRequest : EntityBase
    {

        public DateTimeOffset CreationDate { get; set; }

        public ChangePlanStatus Status { get; set; }
        public string Note { get; set; }
        public long CustomerSimsId { get; set; }
        public virtual CustomerSims CustomerSims { get; set; }


        public long ProductId { get; set; }

        public virtual Product Product { get; set; }

        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }


    }
}
