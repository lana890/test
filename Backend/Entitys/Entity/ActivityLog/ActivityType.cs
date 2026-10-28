using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;
using EskaCommerce.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.ActivityLog
{
    public class ActivityType : EntityBase
    {
        public ActivityType (long id)
        {
            Id = id;
        }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }
    }
}
