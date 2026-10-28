using EskaCMS.Infrastructure.Models;
using System;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.DocumentsNumbering
{
    public class DocumentClassification : EntityBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public virtual User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public virtual User ModifiedBy { get; set; }
        public long? ModifiedById { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
    }
}
