using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Achievements
{
    public class UserAchievments : EntityBase
    {
        public long AchievementId { get; set; }
        public Achievement Achievement { get; set; }
        public long UserId { get; set; }
        public User User { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
}
