using System;

namespace EskaCMS.DocumentsNumbering.Models
{
   
    public class DocumentsNumbersResponseVM
    {
        public string Id { get; set; }
        public string CurrentDocumentNumberId { get; set; }
        public string Name { get; set; }
        public long DocumentClassificationId { get; set; }
        public string DocumentClassification { get; set; }
        public long DocumentTypeId { get; set; }
        public string DocumentType { get; set; }
        public long PreparedById { get; set; }
        public string PreparedBy { get; set; }
        public long ApprovedById { get; set; }
        public string ApprovedBy { get; set; }
        public long GroupId { get; set; }
        public string Group { get; set; }
        public string Language { get; set; }
        public string CountryId { get; set; }
        public string Country { get; set; }
        public string Reference { get; set; }
        public string RevisionNumber { get; set; }
        public string DocumentPublicURL { get; set; }
        public long? DocumentId { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
    }
}
