using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Search.Data;

public class ElasticSearchCustomModelBuilder : ICustomModelBuilder
{
    public void Build(ModelBuilder modelBuilder)
    {
        //modelBuilder.Entity<BackgroundServiceSettings>().HasSeedData(

        //new BackgroundServiceSettings
        //{
        //    Id = "ElasticSearchBackgroundService",
        //    Name = "ElasticSearchBackgroundService",
        //    CreationDate = DateTimeOffset.Now,
        //    ModificationDate = DateTimeOffset.Now,
        //    CreatedById = 1,
        //    TimeSpan = TimeSpan.FromMinutes(5),
        //    IntervalType = IntervalType.Period,
        //    IsActive = true,
        //    PeriodType = BGPeriodType.ByTime,
        //    SiteId = 1
        //},
        // new BackgroundServiceSettings
        // {
        //     Id = "ProductElasticSearchBackgroundService",
        //     Name = "ProductElasticSearchBackgroundService",
        //     CreationDate = DateTimeOffset.Now,
        //     ModificationDate = DateTimeOffset.Now,
        //     CreatedById = 1,
        //     TimeSpan = TimeSpan.FromMinutes(5),
        //     IntervalType = IntervalType.Period,
        //     IsActive = true,
        //     PeriodType = BGPeriodType.ByTime,
        //     SiteId = 1
        // }

        //);
    }
}

