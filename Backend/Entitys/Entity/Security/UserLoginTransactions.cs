using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Security
{
    public class UserLoginTransactions:EntityBase
    {
        // login
        public string UserIP { get; set; }
        public string HostName { get; set; }
        public DateTimeOffset LoginDate { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.UserLoggingType LoggingType { get; set; }
        //public User User { get; set; }
        //public long  UserId { get; set; }

        public long UserSiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.UsersSites UserSite { get; set; }
    }
}
