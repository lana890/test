using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.EmailSender.SMTP.Areas.ViewModels
{
    public class EmailTemplatesViewModel
    {
        public string Id { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]

        public string EmailBody { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public long SiteId { get; set; }
        public string CultureId { get; set; }
        public string Subject { get; set; }
        public long EmailTemplateTypeId { get; set; }
        public bool IsHTML { get; set; }
        public EStatus Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }

        public string? AdditionalInfo { get; set; }
        public string Color { get; set; }
        public string Icon { get; set; }

    }
}
