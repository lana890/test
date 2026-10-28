using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.UsersAndRolesManagement
{
    public class CustomerGroupType : EntityBase
    {

        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public EStatus Status { get; set; }
        public long SiteId { get; set; }
        public Site Site { get; set; }
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }
        public long? ModifiedById { get; set; }
        public List<CustomerGroup> CustomerGroups { get; set; }
    }
}
