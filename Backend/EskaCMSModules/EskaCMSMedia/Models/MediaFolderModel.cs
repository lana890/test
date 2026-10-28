

using System;

namespace EskaCMS.Core.Models
{
    public class MediaFolderModel
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}