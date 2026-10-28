using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Achievements
{
    public class AchievementDetails : EntityBase
    {
        [StringLength(450)]
        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }
        public string Description { get; set; }
        public long AchievementId { get; set; }
        public Achievement Achievement { get; set; }
        public long CultureId { get; set; }
        public SitesCultures Culture { get; set; }


    }
}
