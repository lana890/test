namespace EskaCMS.DocumentsNumbering.Models
{
    public class GetAllVM
    {
        public string Username { get; set; }
        public string AuthToken { get; set; }
        public bool? FlagValue { get; set; }
    }

    public partial class UserDDLVM
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public string FullName { get; set; }
    }


    public class GetByIdVM
    {
        public string Id { get; set; }
    }
}

