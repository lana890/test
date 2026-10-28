using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.UserTaskEnums;

namespace EskaCMS.Entities.Entity.UsersAndRolesManagement
{
    public class UserTasksHistory : EntityBase
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public ETaskPriority Priority { get; set; }
        public ETaskStatus Status { get; set; }
        public ETaskPrivacy Privacy { get; set; }
        public DateTimeOffset? ExpectedStartDate { get; set; }
        public DateTimeOffset? ExpectedEndDate { get; set; }
        public long UserTaskId { get; set; }
        public UserTasks UserTask { get; set; }
    }
}
