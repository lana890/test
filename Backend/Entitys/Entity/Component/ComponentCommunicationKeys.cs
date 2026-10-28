using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Entitys
{
    public class ComponentCommunicationKeys:EntityBase
    {
        public long SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        [Required]
        public string Key { get; set; }
        public long?  PageId { get; set; }
        public Page Page { get; set; }

        public bool IsConFirmed  { get; set; }
        public long? PageTemplateId { get; set; }
        public PageTemplates PageTemplate { get; set; }
        public long CreatedById { get; set; }

        public User CreatedBy { get; set; }

        public DateTime CreationDate { get; set; }

        public DateTime? ModificationDate { get; set; }

        public long? ModifiedById { get; set; }

        public User ModifiedBy { get; set; }


    }
}
