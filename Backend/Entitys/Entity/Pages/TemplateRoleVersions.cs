using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Pages
{
    public class TemplateRoleVersions : EntityBase
    {

        [Required(ErrorMessage = "The {0} field is required.")]
        public string SettingsBody { get; set; }
        public string TemplateComponents { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long PageTemplateId { get; set; }
        public PageTemplates PageTemplate { get; set; }
        public long PageTemplateVersionId { get; set; }
        public TemplateVersions PageTemplateVersion { get; set; }
        public long RoleId { get; set; }
        public Role Role { get; set; }
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }
    }
}
