using EskaCMS.Core.Entities;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Pages.Entities;
using System;
using static EskaCMS.Core.Enums.GeneralEnums;

namespace EskaCMS.UserNavigationTracer.Entites
{
   public class UserNavigation : EntityBase
    {
        public string Slug { get; set; }
        public SlugsTypes SlugsType { get; set; }

        public long UserId { get; set; }
        public virtual User User { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public TimeSpan Period { get; set; }

        public string UserIp { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }


        //public long PageId { get; set; }
        //public Page Page { get; set; }


        public long  VisitId { get; set; } 
        public UserVisit  Visit { get; set; }


        public long SiteId { get; set; }
        public Site Site { get; set; }


       


    }
}
