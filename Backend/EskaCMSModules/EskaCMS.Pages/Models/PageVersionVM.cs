using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Pages.Models
{

    public class PageVersionVM
    {

        public long Id { get; set; }
        public long PageId { get; set; }
        public long VersionId { get; set; }
        public long CreatedById { get; set; }
        public string CreatedByName { get; set; }
        public string PageComponents { get; set; }
        public string SettingsBody { get; set; }
        public string Alias { get; set; }
        public bool IsPublished { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }

    public class ReplaceComponentBody
    {
        public string ComponenetSettingsId { get; set; }
        public long? PageId { get; set; }
        public long? PageTemplateId { get; set; }
        public string NewComponentJson { get; set; } = "{}"; 


    }
}
