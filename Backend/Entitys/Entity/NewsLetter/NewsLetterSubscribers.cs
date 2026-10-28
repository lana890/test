using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Core
{
    public class NewsLetterSubscribers:EntityBase
    {
        public long NewsLetterId { get; set; }
        public NewsLetters NewsLetter { get; set; }

        public long UserId { get; set; }
        public User User { get; set; }
        public bool IsMuted { get; set; } = false;
        public bool IsDirectSubscribed { get; set; }
    }
}
