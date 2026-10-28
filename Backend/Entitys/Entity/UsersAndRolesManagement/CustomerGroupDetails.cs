using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.UsersAndRolesManagement
{
    public class CustomerGroupDetails : EntityBase
    {

        [StringLength(450)]
        public string Name { get; set; }

        public string Description { get; set; }

        public long? CultureId { get; set; }
        public SitesCultures Culture { get; set; }
        public long CustomerGroupId { get; set; }
        public CustomerGroup CustomerGroup { get; set; }

    }
}
