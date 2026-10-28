//using EskaCMS.Entities.Entity.Core;
//using EskaCMS.Infrastructure.Data;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using static EskaCMS.Entities.Enum.GeneralEnums;

//namespace EskaCMS.UserAchievements.Data
//{
//    public class UserAchievementsCustomModelBuilder : ICustomModelBuilder
//    {

//        public void Build(ModelBuilder modelBuilder)
//        {



//            modelBuilder.Entity<BackgroundServiceSettings>().HasSeedData(

//            new BackgroundServiceSettings
//            {
//                Id = "UserAchievementBackgroundService",
//                Name = "UserAchievementBackgroundService",
//                CreationDate = DateTimeOffset.Now,
//                ModificationDate = DateTimeOffset.Now,
//                CreatedById = 1,
//                TimeSpan = TimeSpan.FromMinutes(30),
//                IntervalType = IntervalType.Period,
//                IsActive = true,
//                PeriodType = BGPeriodType.ByTime,
//                SiteId = 1
//            }

//            );


//        }




//    }
//}

