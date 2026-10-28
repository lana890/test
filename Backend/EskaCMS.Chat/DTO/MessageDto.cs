using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Chat.DTO
{
    public class MessageDto
    {
        public long Id { get; set; }
        public long? ReplyId { get; set; }
        public long SenderId { get; set; }
        public string SenderUsername { get; set; }
        public string SenderFullName { get; set; }
        public string SenderImage { get; set; }

        public long? RecipientId { get; set; }
        public string RecipientUsername { get; set; }

        public List<UserGroupDTO> RecipientGroup { get; set; }
        public string Content { get; set; }
        public DateTimeOffset? DateRead { get; set; }
        public DateTimeOffset MessageSent { get; set; }
        public bool IsViolation { get; set; }
        public List<ReadMassageGroupDto>? ReadMassageGroup { get; set; }

        public MessageDto? Reply { get; set; }
        public bool IsForwarded { get; set; }

    }

    public class UserGroupDTO
    {
        public string GroupName { get; set; }
        public long RecipientId { get; set; }
        public string RecipientUsername { get; set; }


    }

    public class MessageUserDto
    {
        public List<GetP2PMessageUser> P2PMessage { get; set; }=new List<GetP2PMessageUser>();
        public List<GetGroupMessageUser> GroupMessage { get; set; } = new List<GetGroupMessageUser>();
        public int UnreadMassagesCount { get; set; }




    }

    public class GetP2PMessageReplay
    {
        public long SenderId { get; set; }
        public string SenderUsername { get; set; }
        public long? RecipientId { get; set; }
        public string RecipientUsername { get; set; }
        public string Content { get; set; }
        public DateTimeOffset MessageSent { get; set; }
        public DateTimeOffset? DateRead { get; set; }
    }

    public class GetGroupMessageReplay
    {
        public long SenderId { get; set; }
        public string SenderUsername { get; set; }
        public long GroupId { get; set; }

        public string Content { get; set; }

        public DateTimeOffset MessageSent { get; set; }

        public List<ReadMassageGroupDto>? ReadMassageGroupDto { get; set; }
    }

    public class GetP2PMessageUser
    {
        public long SenderId { get; set; }
        public string SenderUsername { get; set; }
        public long? RecipientId { get; set; }
        public string RecipientUsername { get; set; }
        public string Content { get; set; }

        public DateTimeOffset MessageSent { get; set; }
        public DateTimeOffset? DateRead { get; set; }
        public int CountUnReadMassage { get; set; }
        public long UserFriendId { get; set; }
        public GetP2PMessageReplay? Reply { get; set; }
        public bool IsForwarded { get; set; }
    }

    public class GetGroupMessageUser
    {
        public long SenderId { get; set; }
        public string SenderUsername { get; set; }
        public long GroupId { get; set; }

        public string Content { get; set; }

        public DateTimeOffset MessageSent { get; set; }
        public int CountUnReadMassage { get; set; }

        public List<ReadMassageGroupDto>? ReadMassageGroupDto { get; set; }
        public GetGroupMessageReplay? Reply { get; set; }
        public bool IsForwarded { get; set; }
    }

    public class ReadMassageGroupDto
    {
        public long UserId { get; set; }
        public string FullName { get; set; }
        public DateTimeOffset? DateRead { get; set; }

    }
}
