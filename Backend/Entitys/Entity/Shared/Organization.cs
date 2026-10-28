using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Core
{
    public partial class Organization : EntityBase
    {
        public Organization(long id)
        {
            Id = id;
        }
        public Organization()
        {
        }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(500)]
        public string Name { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(500)]
        public string Description { get; set; }
        public long? ThumbnailId { get; set; }
        public virtual Media Thumbnail { get; set; }
        
        public string CountryId { get; set; }
        public long AddressId { get; set; }
        public long CategoryId { get; set; }

        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }

        public DateTimeOffset StatusDate { get; set; }

        public string ContactEmail { get; set; }
        public string ContactMobileNo { get; set; }
        public int? CoreCompanyId { get; set; }

        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
        public virtual Category Category { get; set; }
        public ICollection<Site> Sites { get; set; }
    }
}
