using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity;
using System.Collections.Generic;

namespace EskaCMS.Entities.Entity.Payments
{
    public class PaymentProvider : EntityBaseWithTypedId<string>
    {
        public PaymentProvider(string id)
        {
            Id = id;
        }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }

        /// <summary>
        /// Additional setting for specific provider. Stored as json string.
        /// </summary>
        public string AdditionalSettings { get; set; }

        public bool IsEnabled { get; set; }
        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }
    }
}
