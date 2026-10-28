namespace EskaCMS.DocumentsNumbering.Models
{
    public class DocumentClassificationAddVM
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
    public class DocumentClassificationVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long CreatedById { get; set; }
        public string CreatedBy { get; set; }
    }
}
