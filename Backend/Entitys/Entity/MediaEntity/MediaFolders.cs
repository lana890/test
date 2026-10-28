using EskaCMS.Entities.Entity.MediaEntity;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Core
{
    

    [Serializable]
    public class MediaFolders : EntityBase
    {


        /// <summary>
        /// Gets/sets the optional parent id.
        /// </summary>
        public long? ParentId { get; set; }

        /// <summary>
        /// Gets/sets the folder name.
        /// </summary>
        public long? Order { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        //public virtual MediaFolders Parent { get; set; }
        public  MediaFolders Parent { get; set; }
        public long SiteId { get; set; }
        public virtual Site Site { get; set; }
       
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }




        //public virtual Media Media { get; set; }
        public ICollection<MediaFoldersDetails> MediaFoldersDetails { get; set; } //= new List<MediaFoldersDetails>();

        public IList<Media> Media { get; set; } = new List<Media>();
        public ICollection<FolderRole> FolderRoles { get; set; } = new List<FolderRole>();


        public List <MediaFolders> ChildFolders { get; set; }
        public EStatus Status { get; set; }
    }

   
}
