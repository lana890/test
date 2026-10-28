using System;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.ActivityLog
{
    public class Activity : EntityBase
    {
        public long ActivityTypeId { get; set; }

        public ActivityType ActivityType { get; set; }

        public long UserId { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public long EntityId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string EntityTypeId { get; set; }
    }
}
