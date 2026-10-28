
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity
{
    [Serializable]
    public sealed class MediaVM : MediaBase
    {
        /// <summary>
        /// Gets/sets the user defined properties.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets/sets the available versions.
        /// </summary>
        public IList<MediaVersion> Versions { get; set; } = new List<MediaVersion>();
    }

    [Serializable]
    public abstract class MediaBase 
    {
        public long Id { get; set; }
        /// <summary>
        /// Gets/sets the unique id.
        /// </summary>
     //   public Guid Id { get; set; }

        /// <summary>
        /// Gets/sets the optional folder id.
        /// </summary>
        public long? FolderId { get; set; }

        /// <summary>
        /// Gets/sets the media type.
        /// </summary>
        public MediaType Type { get; set; }

        /// <summary>
        /// Gets/sets the filename.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Filename { get; set; }


        public IList<MediaDetails> MediaDetails { get; set; }

        /// <summary>
        /// Gets/sets the content type.
        /// </summary>
        [Required]
        [StringLength(256)]
        public string ContentType { get; set; }

        /// <summary>
        /// Gets/sets the optional title.
        /// </summary>
        [StringLength(128)]
        public string Title { get; set; }

        /// <summary>
        /// Gets/sets the optional alt text.
        /// </summary>
        [StringLength(128)]
        public string AltText { get; set; }

        /// <summary>
        /// Gets/sets the optional description.
        /// </summary>
        [StringLength(512)]
        public string Description { get; set; }

        /// <summary>
        /// Gets/sets the file size in bytes.
        /// </summary>
        public long Size { get; set; }


        public long? Order { get; set; }

        /// <summary>
        /// Gets/sets the public url.
        /// </summary>
        public string PublicUrl { get; set; }

        /// <summary>
        /// Gets/sets the optional width. This only applies
        /// if the media asset is an image.
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// Gets/sets the optional height. This only applies
        /// if the media asset is an image.
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// Gets/sets the created date.
        /// </summary>
        public DateTimeOffset CreationDate { get; set; }

        /// <summary>
        /// Gets/sets the last modification date.
        /// </summary>
        public DateTimeOffset? ModificationDate { get; set; }

        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public long SiteId { get; set; }

        public long CultureId { get; set; }

        public long CreatedById { get; set; }
        public long? ModifiedById { get; set; }

        public bool? Force { get; set; }

        public EStatus Status { get; set; }

        public string AltUrl { get; set; }
    }
}