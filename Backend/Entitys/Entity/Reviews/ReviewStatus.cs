using System.ComponentModel;

namespace EskaCMS.Entities.Entity
{
    public enum ReviewStatus
    {
        Pending = 1,

        Approved = 5,

        [Description("Not Approved")]
        NotApproved = 8,
        Deleted = 9
    }
}
