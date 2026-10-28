using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.Achievements.Enums.AchievmentEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.UserAchievements.Models
{
    public class AchievementUpdateVM
    {
        [Required]

        public long Id { get; set; }
        [Required]
        [Range(1, 2)]
        public EStatus Status { get; set; }
        [Required]
        public AchievmentType Type { get; set; }
        public AchievementObtainType? ObtainType { get; set; }
        [Required]
    
        public int ExceededCount { get; set; }
        [Required]
        public List<AchievementDetailsVM> AchievementDetails { get; set; }
        public long? ThumbnailId { get; set; }
       
    }
}
