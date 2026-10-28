using EskaCMS.Infrastructure.Data;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using EskaCMS.Entities.Entity;
//using EskaCMS.Entities.Entity.Core;

//namespace EskaCMS.HOC.Data
//{
//    public class HOCSeedData
//    {
//        public static void SeedData(ModelBuilder builder)
//        {
           
//            builder.Entity<BackgroundServiceSettings>().HasSeedData(

//                new BackgroundServiceSettings
//                {
//                    SiteId = 1,
//                    TimeSpan = TimeSpan.FromDays(1),
//                    DayOfWeek = DayOfWeek.Sunday,
//                    CreatedById = 1,
//                    CreationDate = DateTimeOffset.Now,
//                    ModificationDate = DateTimeOffset.Now,
//                    IntervalDate = DateTimeOffset.Now,
//                    IntervalType =EskaCMS.Entities.Enum.GeneralEnums.IntervalType.Period,
//                    Name = "ExportDataBaseBackgrounService",
//                    Id = "ExportDataBaseBackgrounService",
//                    MonthOfYear = 12,
//                    WeekOfMonth = 4,
//                    IsActive = false,
//                    PeriodType = EskaCMS.Entities.Enum.GeneralEnums.BGPeriodType.Daily
//                }

//                );

//        }
//    }
//    }

