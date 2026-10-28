using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.ViewModels
{
    public class MediaFolderDetailsWithOrderVM
    {
        public long Id { get; set; }
        public string Title { get; set; }

        public string Description { get; set; }


        public long? ThumbnailId { get; set; }
        public virtual Media ThumbnailMedia { get; set; }

        public long CultureId { get; set; }
        public virtual SitesCultures Culture { get; set; }
        public long? FolderId { get; set; }
        public virtual MediaFolders Folder { get; set; }

        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }


        public long? Order { get; set; }

    }
}
