
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.DocumentsNumbering
{
    public class DocumentsNumbers : EntityBaseWithTypedId<string>
    {
        public string Id {  get; set; }
        public string Name { get; set; }
        public long DocumentClassificationId { get; set; } // JO
        public long DocumentTypeId { get; set; }
        public string Language { get; set; }
        public string CountryId { get; set; }
        public EskaCMS.Entities.Entity.Addresses.Country Country { get; set; }
        public string Reference { get; set; }
        public string SequenceNumber { get; set; }
        public string RevisionNumber { get; set; }
        public DocumentType DocumentType { get; set; }
        public DocumentClassification DocumentClassification { get; set; }
        public long PreparedById { get; set; }
        public virtual User PreparedBy { get; set; }
        public long ApprovedById { get; set; }
        public virtual User ApprovedBy { get; set; }
        public long GroupId { get; set; }
        public virtual CustomerGroup Group { get; set; }
        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }
        public long ModifiedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public virtual User ModifidedBy { get; set; }
        public virtual ICollection<DocumentNumberHistory> DocumentNumberHistory { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public long SiteId { get; set; }

        public long? SoftCopyId { get; set; }
        public Media SoftCopy { get; set; }

        
    }
}
