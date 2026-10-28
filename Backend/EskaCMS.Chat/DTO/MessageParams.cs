using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Chat.DTO
{
    public class MessageParams
    {
        public string Username { get; set; }
        public string Container { get; set; } = "Unread";
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
