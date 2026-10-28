using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.NewsLetter
{
    public class NewsLetterViwers : EntityBase
    {

        public DateTimeOffset CreationDate { get; set; }
        public long NewsLetterStatisticId { get; set; }
        public virtual NewsLetterStatistics NewsLetterStatistic { get; set; }

        public long UserId { get; set; }

        public virtual User User { get; set; }
    }
}
