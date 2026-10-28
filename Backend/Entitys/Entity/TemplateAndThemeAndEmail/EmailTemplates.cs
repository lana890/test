using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EskaCMS.Entities.Entity.Core
{
    public  class EmailTemplates : EntityBaseWithTypedId<string>
    {

        public EmailTemplates(string id)
        {
            Id = id;
        }
        [Required(ErrorMessage = "The {0} field is required.")]

        public string EmailBody { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public long SiteId { get; set; }
        public string CultureId { get; set; }
        public string Subject { get; set; }
        public long EmailTemplateTypeId { get; set; }
        public bool IsHTML { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long CreatedById { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public Enum.GeneralEnums.EStatus Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }

        public string? AdditionalInfo { get; set; }
        public string Color { get; set; }
        public string Icon { get; set; }

        public virtual EmailTemplateType EmailTemplateType { get; set; }
        public ICollection<NewsLetters> NewsLetters { get; set; }

    }
}
