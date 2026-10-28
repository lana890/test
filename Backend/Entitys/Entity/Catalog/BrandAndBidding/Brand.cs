using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EskaCMS.Entities.Entity.Core;

using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class Brand : EntityBase
    {

        public Enum.GeneralEnums.EStatus Status { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public long SiteId { get; set; }
        public Site Site { get; set; }

        public long CreatedById { get; set; }

        public User CreatedBy { get; set; }

        public DateTimeOffset CreationDate { get; set; }

        public DateTimeOffset? ModificationDate { get; set; }

        public long? ModifiedById { get; set; }

        public User ModifiedBy { get; set; }

        public long? ThumbnailId { get; set; }
        public Media Thumbnail { get; set; }

        public DateTimeOffset? PublishDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        // public IList<Brand> Children { get; set; }

        public List<BrandDetails> BrandDetails { get; set; }

    }
}
