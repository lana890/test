using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Security
{
    public class UserWorkingHours :EntityBase
    {
        public UserWorkingHours()
        {
            Days = new List<WorkingHoursDay>();
        }

        public virtual CustomerGroup CustomerGroup { get; set; }
        public long CustomerGroupId { get; set; }
        public TimeSpan  FromTime { get; set; }
        public TimeSpan ToTime { get; set; }
        public User CreatedBy { get; set; }
        public long CreatedById { get; set; }
        public User ModifiedBy { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public List<WorkingHoursDay> Days { get; set; }
    }
}
