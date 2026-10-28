using EskaCMS.Infrastructure.Models;
using System;
using EskaCMS.Entities.Entity.Core;


namespace EskaCMS.Entities.Entity.BlogArchive
{
    public partial class PostsCategories : EntityBase
    {
        public long PostId { get; set; }
        public long CategoryId { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public virtual Posts Post { get; set; }
        public virtual Category Category { get; set; }

        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
    }
}
