
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Entities.Entity.Core
{
    public partial class CategoryTypes : EntityBase
    {


        public CategoryTypes(long id)
        {
            Id = id;
        }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }
        public string Description { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
        //public Site Sites { get; set; }
        //public long? SiteId { get; set; }



    }
}
