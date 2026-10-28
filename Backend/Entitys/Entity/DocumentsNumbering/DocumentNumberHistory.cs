
using EskaCMS.Infrastructure.Models;
using System;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.DocumentsNumbering
{
    public class DocumentNumberHistory : EntityBase
    {
        public DocumentNumberHistory()
        {
                    
        }
        public DocumentNumberHistory(DocumentsNumbers DocNumber)
        {
                this.DocumentNumberId = DocNumber.Id;
            this.DocumentNumberId = DocNumber.Id;
            this.SoftCopyId = DocNumber.SoftCopyId;
            this.SiteId = DocNumber.SiteId;
            this.DocModificationDate = DateTimeOffset.Now;
            this.CreationDate = DateTimeOffset.Now;
            this.DocModifiedById = DocNumber.ModifiedById;
            this.CreatedById = DocNumber.CreatedById;
            this.GroupId = DocNumber.GroupId;
            this.ApprovedById = DocNumber.ApprovedById;
            this.PreparedById = DocNumber.PreparedById;
            this.RevisionNumber = DocNumber.RevisionNumber;
            this.SequenceNumber = DocNumber.SequenceNumber;
            this.Reference = DocNumber.Reference;
            this.CountryId = DocNumber.CountryId;
            this.Language = DocNumber.Language;
            this.DocumentTypeId = DocNumber.DocumentTypeId;
            this.DocumentClassificationId = DocNumber.DocumentClassificationId;
            this.Name = DocNumber.Name;
        }
        public string CurrentDocumentNumber { get; set; }
        public string DocumentNumberId { get; set; }
        public DocumentsNumbers DocumentNumber { get; set; }
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
        public long DocModifiedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? DocModificationDate { get; set; }
        public virtual User DocModifidedBy { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public long SiteId { get; set; }

        public long? SoftCopyId { get; set; }
        public Media SoftCopy { get; set; }
    }
}
