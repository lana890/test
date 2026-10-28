
using EskaCMS.Infrastructure.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using EskaCMS.Entities.Entity.Core;


namespace EskaCMS.Entities.Entity.Pages

{

    public partial class PageTemplates : EntityBase
    {
        public PageTemplates()
        {
            CreationDate = DateTimeOffset.Now;
        }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public long SiteId { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.ETemplateTypes Type { get; set; }

        public long CreatedById { get; set; }


        public DateTimeOffset CreationDate { get; set; }

        public long? ModifiedById { get; set; }

        public DateTimeOffset? ModificationDate { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }

        public string AddtionalMetaTags { get; set; }
        public long CurrentVersionId { get; set; }
        public TemplateVersions CurrentVersion { get; set; }

        public DateTimeOffset StatusDate { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
        public ICollection<TemplateVersions> PageTemplateVersions { get; set; }
        public ICollection<TemplateAssets> TemplateAssets { get; set; }

    }
}
