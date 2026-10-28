using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Core
{
  public  class DCMSLog : EntityBase
    {

       public DCMSLog ()
        {
            Date = DateTimeOffset.Now; 
        }

        public LogTypes LogType { get; set; } 

        public string Category { get; set; }
        public long? EventId { get; set; }
        public string EventName { get; set; }
        public string Scope { get; set; }
        public string Module { get; set; }
        public string Exception { get; set; }
        public long? ParentId { get; set; }
        public string Message { get; set; }
        public string AdditionInfo { get; set; }
        public long? SiteId { get; set; }

        public EPlatformSource Channel { get; set; }
        public Site Site { get; set; }
        public User User { get; set; }
        public long? UserId { get; set; }

        public DateTimeOffset Date { get; set; }
    }
}
