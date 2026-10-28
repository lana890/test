using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using System.ComponentModel.DataAnnotations.Schema;

namespace EskaCMS.Entities.Entity.UserDashboard
{
    public class UserNavigations : EntityBase
    {
        public string Slug { get; set; }
        public SlugsTypes SlugsType { get; set; }


        public string PreviousSlug { get; set; }
        public SlugsTypes? PreviousSlugsType { get; set; }


        public long UserId { get; set; }
        public virtual User User { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public TimeSpan Period { get; set; }

        public string UserIp { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }


        //public long PageId { get; set; }
        //public Page Page { get; set; }


        public long VisitId { get; set; }
        public UserVisits Visit { get; set; }


        public long SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }


        public string BrowserName { get; set; }
        public string BrowserVersion { get; set; }
        public BrowserType Browser { get; set; }





    }
}
