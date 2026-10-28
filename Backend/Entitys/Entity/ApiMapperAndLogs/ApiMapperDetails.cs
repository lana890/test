using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EskaCMS.Entities.Entity.Core
{
    public class ApiMapperDetails : EntityBase
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string ResponsePath { get; set; }
        public long ApiMapperId { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long CreatedById { get; set; }

        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public ApiMapper ApiMapper { get; set; }

    }
}
