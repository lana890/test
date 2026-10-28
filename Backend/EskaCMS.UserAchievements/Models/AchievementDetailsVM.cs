using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.UserAchievements.Models
{
    public class AchievementDetailsVM
    {
        public long? Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }


        [Required]
        public long CultureId { get; set; }
        
        public string CultureCode { get; set; }
    }
}
