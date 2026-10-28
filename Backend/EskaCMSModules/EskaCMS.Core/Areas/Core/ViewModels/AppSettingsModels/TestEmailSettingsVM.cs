using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.ViewModels.AppSettingsModels
{
    public class TestEmailSettingsVM
    {
        public string SmtpUsername { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]

        public string SmtpPassword { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]

        public string SmtpServer { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]

        public string SmtpSenderEmail { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]

        public int SmtpPort { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]

        public bool RequiresAuthentication { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public string EmailTo { get; set; }
        public string  Body { get; set; }



    }
}
