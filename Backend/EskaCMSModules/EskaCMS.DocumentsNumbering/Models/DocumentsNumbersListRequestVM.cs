using System;

namespace EskaCMS.DocumentsNumbering.Models
{
   
    public class DocumentsNumbersListRequestVM
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public long? DocumentClassificationId { get; set; }
        public long? DocumentTypeId { get; set; }
        public long? PreparedById { get; set; }
        public long? ApprovedById { get; set; }
        public long? GroupId { get; set; }
        public string CountryId { get; set; }
        public string RevisionNumber { get; set; }
        public string Language { get; set; }
        public string Year { get; set; }
    }
}
