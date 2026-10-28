using EskaCMS.DocumentsNumbering.Enums;

namespace EskaCMS.DocumentsNumbering.Models
{
    public class DocumentsNumbersAddVM
    {
        public string Name { get; set; }
        public long DocumentClassificationId { get; set; }
        public long DocumentTypeId { get; set; }
        public string Language { get; set; }
        public string Reference { get; set; }
        public string RevisionNumber { get; set; }
        public long PreparedById { get; set; }
        public long? ApprovedById { get; set; }
        public long? GroupId { get; set; }
        public string CountryId { get; set; }
        public string CountryCode { get; set; }
        public string GroupName { get; set; }
    }

    public class LinkSoftCopyVM
    {
        public long SoftCopyId { get; set; }    
        public string DocumentNumberId { get; set; }
    }
}
