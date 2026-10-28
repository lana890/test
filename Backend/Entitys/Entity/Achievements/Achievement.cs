using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.Achievements.Enums.AchievmentEnums;

namespace EskaCMS.Entities.Entity.Achievements
{
    public class Achievement : EntityBase
    {
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public AchievmentType Type { get; set; }
        public AchievementObtainType? ObtainType { get; set; }
        public int ExceededCount { get; set; }
        public List<AchievementDetails> AchievementDetails { get; set; }
        public long? ThumbnailId { get; set; }
        public virtual Media Thumbnail { get; set; }
        public long SiteId { get; set; }
        public Site Site { get; set; }
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }

    }
}
