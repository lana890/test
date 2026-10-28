using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Core.Models
{
    public class EmailTemplateVM
    {

        public string Id { get; set; }
        public long SiteId { get; set; }
        public string CultureId { get; set; }
        public string Subject { get; set; }

        public long EmailTemplateTypeId { get; set; }
        public bool IsHTML { get; set; }
        public EmailTemplateTypesVM EmailTemplateTypes { get; set; }
        public string EmailBody { get; set; }
        public string Color { get; set; }
        public string Icon { get; set; }
        public string? AdditionalInfo { get; set; }

    }



}
