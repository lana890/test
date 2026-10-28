using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.HOC.Data
{
    internal class HocCustomeModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
           // modelBuilder.Entity<BackgroundServiceSettings>().HasSeedData(

           //              new BackgroundServiceSettings
           //              {
           //                  Id = "CurrentUsersBackgroundServices",
           //                  Name = "CurrentUsersBackgroundServices",
           //                  CreationDate = DateTimeOffset.Now,
           //                  ModificationDate = DateTimeOffset.Now,
           //                  CreatedById = 1,
           //                  TimeSpan = TimeSpan.FromMinutes(60),
           //                  IntervalType = IntervalType.Period,
           //                  IsActive = true,
           //                  PeriodType = BGPeriodType.ByTime,
           //                  SiteId = 1
           //              }

           //              );
           // modelBuilder.Entity<BackgroundServiceSettings>().HasSeedData(

           // new BackgroundServiceSettings
           // {
           //     Id = "StockQuentityBackgroundServices",
           //     Name = "StockQuentityBackgroundServices",
           //     CreationDate = DateTimeOffset.Now,
           //     ModificationDate = DateTimeOffset.Now,
           //     CreatedById = 1,
           //     TimeSpan = TimeSpan.FromMinutes(1),
           //     IntervalType = IntervalType.Period,
           //     IsActive = true,
           //     PeriodType = BGPeriodType.ByTime,
           //     SiteId = 1
           // }

           // );

           // modelBuilder.Entity<BackgroundServiceSettings>().HasSeedData(

           //new BackgroundServiceSettings
           //{
           //    Id = "PayTabsRecuringBackgroundServices",
           //    Name = "PayTabsRecuringBackgroundServices",
           //    CreationDate = DateTimeOffset.Now,
           //    ModificationDate = DateTimeOffset.Now,
           //    CreatedById = 1,
           //    TimeSpan = TimeSpan.FromMinutes(1),
           //    IntervalType = IntervalType.Period,
           //    IsActive = true,
           //    PeriodType = BGPeriodType.ByTime,
           //    SiteId = 1
           //}

           //);


        }
    }
}

