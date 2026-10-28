using EskaCMS.Infrastructure.Models;

namespace EskaCMS.NumberingTool.Entities
{
    public class DocumentNumber : EntityBase
    {
        public string Name { get; set; }
        public string Department { get; set; }
        public long DocumentTypeId { get; set; }
        public DocumentType DocumentType { get; set; }
        public DocumentType DocumentType { get; set; }
    }
}
