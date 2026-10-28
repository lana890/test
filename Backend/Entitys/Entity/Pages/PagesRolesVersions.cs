using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Pages

{
    public class PagesRolesVersions : EntityBase
    {

        public long PageVersionId { get; set; }

        public PageVersions PageVersion { get; set; }
        public Page Page { get; set; }
        public Role Role { get; set; }
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long PageId { get; set; }
        public long RoleId { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public string SettingsBody { get; set; }
        public string PageComponents { get; set; }
    }
}
