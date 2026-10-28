using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EskaCMS.Entities.Entity.Core
{
    public partial class EmailTemplateType:EntityBase
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long CreatedById { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
    }
}
