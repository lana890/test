using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models.TaskModels
{
    public class UserProfileTasks
    {
        public string UserId { get; set; }

        public SmartTableParam SmartTable { get; set; }
    }
}
