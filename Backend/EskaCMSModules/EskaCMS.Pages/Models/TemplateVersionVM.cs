using EskaCMS.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Pages.Models
{
 
    public class TemplateVersionVM
    {
         public long TemplateId { get; set; }

        public long Id { get; set; }
        public long VersionId { get; set; }
        public string SettingsBody { get; set; }
        public string Css { get; set; }
        public string Alias { get; set; }
        public string Scripts { get; set; }

        public List<Entities.Entity.ImportedFilesVM> ImportFilesList { get; set; } = new List<Entities.Entity.ImportedFilesVM>(); // for Imported files Data
        public long CreatedById { get; set; }
        public string CreatedByName { get; set; }
        public DateTimeOffset CreationDate { get; set; }

        public string TemplateComponents { get; set; }

    }
}
