
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.SitesAndCultures;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EskaCMS.Entities.Entity.Core
{
    public partial class Site : EntityBase
    {
        public Site(long id)
        {
            Id = id;
        }
        public Site()
        {
        }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(500)]
        public string Name { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(500)]
        public string Description { get; set; }

       // [ForeignKey(name: "Thumbnail")]
        public long? ThumbnailId { get; set; }

        public virtual Media Thumbnail { get; set; }
        public string Css { get; set; }

        public long? OrganizationId { get; set; }
        public long? CoreBranchId { get; set; }
        public bool IsADAuthRequired { get; set; }
        public long CreatedById { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }

        public DateTimeOffset StatusDate { get; set; }

         //public string DefaultSlug { get; set; }

        public long DefaultParentPageId { get; set; }

        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
        public Organization Organization { get; set; }
        public ICollection<UsersSites> UserSites { get; set; }

        public ICollection<Theme> SiteThemes { get; set; }

        public ICollection<SiteSettings> SiteSettings { get; set; }
        public ICollection<SitesHostNames> SitesHostNames { get; set; }
        public ICollection<SitesCultures> SitesCultures { get; set; }

        public string Script { get; set; }

        public string? CountryId { get; set; }
        public Country Country { get; set; }
        public bool EnableTwoFactorAuthentication { get; set; }

    }
}
