using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.Achievements.Enums.AchievmentEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.UserAchievements.Models
{
    public class AchievementListVM
    {
        public long Id { get; set; }
        public EStatus Status { get; set; }
       
        public AchievmentType Type { get; set; }
        public AchievementObtainType? ObtainType { get; set; }
     
 
        public int ExceededCount { get; set; }
 
        public List<AchievementDetailsVM> AchievementDetails { get; set; }
        public long? ThumbnailId { get; set; }
        public string Image { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
}
