namespace EskaCMS.Core.ViewModels
{
    public class UserListVM
    {
        public long Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }

    }
    public class PreparedByVM
    {
        public long PreparedById { get; set; }
        public string PreparedByName { get; set; }
        public long? ApprovedById { get; set; }
        public string ApprovedBy { get; set; }

    }

}
