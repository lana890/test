using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Core
{
    public class MediaFoldersDetails:EntityBase
   {

        [Required]
        [StringLength(128)]
        public string Title { get; set; }
        [StringLength(512)]

        public string Description { get; set; }

        
        public long? ThumbnailId { get; set; }
        public Media Thumbnail { get; set; }
        
        public long CultureId { get; set; }
        public virtual SitesCultures Culture { get; set; }
        public long? FolderId { get; set; }
        public virtual MediaFolders Folder { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
       

    }
}
