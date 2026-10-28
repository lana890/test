using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Core
{
    public class NewsLetterMedias:EntityBase
    {
        public long MediaId { get; set; }
        public Media Media { get; set; }
        public long NewsLetterId { get; set; }
        public NewsLetters NewsLetter { get; set; }
    }
}
