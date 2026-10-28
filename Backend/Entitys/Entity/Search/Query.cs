using System;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.Search
{
    public class Query : EntityBase
    {
         [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(500)]
        public string QueryText { get; set; }

        public int ResultsCount { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public long SiteId { get; set; }

        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }
    }
}
