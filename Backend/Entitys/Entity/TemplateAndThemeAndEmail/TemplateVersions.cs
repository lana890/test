using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Pages
{
    public partial class TemplateVersions : EntityBase
    {


        public TemplateVersions()
        {
            //   Page = new HashSet<DcmsPages>();
        }

        public long? TemplateId { get; set; }
        public virtual PageTemplates Template { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public long VersionId { get; set; }
        public string SettingsBody { get; set; }
        public string Css { get; set; }
        public string Alias { get; set; }
        public string Scripts { get; set; }
        
        [NotMapped]
        public List<ImportedFilesVM> ImportFilesList { get; set; } // for Imported files Data
        public IList<TemplateRoleVersions> TemplateRoleVersions { get; set; } // for Imported files Data

        public IList<TemplateImportedFiles> ImportedFiles { get; set; } // forign Key


        public string TemplateComponents { get; set; }
        public virtual User CreatedBy { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
}
