using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using EskaCMS.Entities.Entity.Core;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace EskaCMS.Entities.Entity.Pages
{
    public partial  class PageVersions : EntityBase
    {

        public long? PageId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public long VersionId { get; set; }
        public long CreatedById { get; set; }
        public string Alias { get; set; }
        public string SettingsBody { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public DateTimeOffset CreationDate { get; set; }
        public virtual Page Page { get; set; }
        public virtual User CreatedBy { get; set; }
        public string PageComponents { get; set; }

    }
}
