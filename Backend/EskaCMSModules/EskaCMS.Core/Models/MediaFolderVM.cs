

using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Core.Models
{
    [Serializable]
    public class MediaFolderVM
    {
        /// <summary>
        /// Gets/sets the unique id.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets/sets the optional parent id.
        /// </summary>
        public long? ParentId { get; set; }


        /// <summary>
        /// Gets/sets the folder name.
        /// </summary>
      
        public string Title { get; set; }

        /// <summary>
        /// Gets/sets the optional description.
        /// </summary>
        [StringLength(512)]
        public string Description { get; set; }

        /// <summary>
        /// Gets/sets the created date.
        /// </summary>
        public DateTimeOffset CreationDate { get; set; }
        public long CreatedById { get; set; }

        public long SiteId { get; set; }
        public long CultureId { get; set; }

        public long? ThumbnailId { get; set; }

        public List<MediaFoldersDetails> MediaFoldersDetails { get; set; }

        public long? Order { get; set; }

    }

    [Serializable]
    public class MediaFolderSimple
    {
        /// <summary>
        /// Gets/sets the unique id.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets/sets the folder name.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Name { get; set; }
    }

    [Serializable]
    public class RequestMediaFolderVM
    {
     
        public long Id { get; set; }

   
        public long? ParentId { get; set; }
      

        public long? ThumbnailId { get; set; }

        public List<RequestMediaFoldersDetails> MediaFoldersDetails { get; set; }

        public long? Order { get; set; }

    }

    public class RequestMediaFoldersDetails 
    {

        [Required]
        [StringLength(128)]
        public string Title { get; set; }
        [StringLength(512)]

        public string Description { get; set; }


        public long? ThumbnailId { get; set; }

        public long CultureId { get; set; }

    }
}