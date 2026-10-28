using EskaCMS.Core.Entities;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
namespace EskaCMS.UserNavigationTracer.Entites
{
    public class UserVisit : EntityBase
    {
        
        
        public DateTimeOffset CreationDate { get; set; }

      

        public long SiteId { get; set; }
        public virtual Site Site { get; set; }


        public long UserId { get; set; }
        public virtual User User { get; set; }

        public IList<UserNavigation> UserNavigations { get; set; }
    }
}
