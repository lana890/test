using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    // 
    public class UpsertEmailSettingsVM
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string  Name { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public string SmtpUsername { get; set; }
      
        public string SmtpPassword { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]

        public string SmtpServer { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [EmailAddress]

        public string SmtpSenderEmail { get; set; }

        public string SmtpSenderEmailAdmin { get; set; } = null;
        [Required(ErrorMessage = "The {0} field is required.")]
        public int SmtpPort { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]

        public bool RequiresAuthentication { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]

        public bool IsDefault { get; set; }
        public EStatus Status { get; set; }
    }
}
