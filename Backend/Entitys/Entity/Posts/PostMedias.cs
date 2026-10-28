using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EskaCMS.Entities.Entity.BlogArchive
{

    public partial class PostMedias : EntityBase
    {
        public long PostId { get; set; }

        public virtual EskaCMS.Entities.Entity.Core.Media Media { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public long MediaId { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public virtual Posts Post { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }

     
    }
}
