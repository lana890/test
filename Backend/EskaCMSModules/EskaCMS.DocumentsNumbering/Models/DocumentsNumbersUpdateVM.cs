namespace EskaCMS.DocumentsNumbering.Models
{
    public class DocumentsNumbersUpdateVM
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Reference { get; set; }
        public string RevisionNumber { get; set; }
        public long? PreparedById { get; set; }
        public long? ApprovedById { get; set; }
        public long? GroupId { get; set; }
        public long? SoftCopyId { get; set; }
        public string CountryId { get; set; }
    }
}
