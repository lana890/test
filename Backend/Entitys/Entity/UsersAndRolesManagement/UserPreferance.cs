using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.UsersAndRolesManagement
{
    public class UserPreferance : EntityBase
    {
        public string DateFormat { get; set; }
        public string TimeFormat { get; set; }
        public string FontSize { get; set; }
        public long? ThemeId { get; set; }
        public Theme Theme { get; set; }
        public long SiteId { get; set; }
        public Site Site { get; set; }
        public long UserId { get; set; }
        public User User { get; set; }

    }
}
