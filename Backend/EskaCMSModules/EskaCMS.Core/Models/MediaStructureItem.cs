

using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Models
{
    [Serializable]
    public class MediaStructureItem : StructureItem<MediaStructure, MediaStructureItem>
    {
        /// <summary>
        /// Gets/sets the folder name.
        /// </summary>
        public string Name { get; set; }

        public string Description { get; set; }

        public EStatus Status { get; set; }
        public long? ThumbnailId { get; set; }
        public virtual Media ThumbnailMedia { get; set; }

        public string Code { get; set; }
        public long? Order { get; set; }

        /// <summary>
        /// Gets/sets the number of child folders in the folder.
        /// </summary>
        public int FolderCount { get; set; }

        /// <summary>
        /// Gets/sets the number of media items in the folder.
        /// </summary>
        public int MediaCount { get; set; }

        /// <summary>
        /// Gets/sets the created date.
        /// </summary>
        /// 

        public IList<MediaFoldersDetails> MediaFoldersDetails { get; set; } = new List<MediaFoldersDetails>();

        public DateTime Created { get; set; }
        public long CulturId { get; set; }
        /// <summary>
        /// Default constructor.
        /// </summary>
        public MediaStructureItem()
        {
            Items = new MediaStructure();
        }
    }
}