using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Entities.Enum.UserTaskEnums;

namespace EskaCMS.Entities.Entity.UsersAndRolesManagement
{
    public class UserTasks : EntityBase
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int  Order { get; set; }
        public DateTimeOffset? ExpectedStartDate { get; set; }
        public DateTimeOffset? ExpectedEndDate { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public DateTimeOffset? StatusDate { get; set; }
        public ETaskPriority Priority { get; set; }
        public ETaskStatus Status { get; set; }
        public ETaskPrivacy Privacy { get; set; }
        public long SiteId { get; set; }
        public Site Site { get; set; }
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }
        public long? ParentId { get; set; }
        public UserTasks Parent { get; set; }
        public long? CategoryId { get; set; }
        public Category Category { get; set; }
        public ICollection<UserTasks> ChildrenTasks { get; set; }
        public ICollection<UserTasksHistory> UserTasksHistory { get; set; }
       
    }
}
