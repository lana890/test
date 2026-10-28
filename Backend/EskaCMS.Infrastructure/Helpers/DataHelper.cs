using EskaCMS.Infrastructure.Enums;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Infrastructure.Helpers
{
    public class DateHelper
    {


        public static bool IsCurrentDay(DateTimeOffset DateTime)
        {
            return DateTime.Date == DateTimeOffset.Now.Date;
        }



        public static bool IsPassedAYear(DateTimeOffset DateTime)
        {
            return DateTime.AddYears(1).CompareTo(DateTimeOffset.Now) < 0;
        }

        public static bool IsPassedAMonth(DateTimeOffset DateTime)
        {
            return DateTime.AddMonths(1).CompareTo(DateTimeOffset.Now) < 0;
        }

        public static bool IsPassedAWeek(DateTimeOffset DateTime)
        {
            return DateTime.AddDays(7).CompareTo(DateTimeOffset.Now) < 0;
        }
        public static bool IsPassedADay(DateTimeOffset DateTime)
        {
            return DateTime.AddDays(1).CompareTo(DateTimeOffset.Now) < 0;
        }


        public static bool IsPassedTimeSpan(DateTimeOffset DateTime, TimeSpan timeSpan)
        {
            return DateTime.Add(timeSpan).CompareTo(DateTimeOffset.Now) < 0;
        }

        public static bool IsDayOfWeek(DateTimeOffset DateTime, DayOfWeek Day)
        {
            return DateTime.DayOfWeek == Day;
        }


        public static bool IsMonthOfYear(DateTimeOffset DateTime, int Month)
        {
            return DateTime.Month == Month;
        }


        public static bool IsWeekOfMonth(DateTimeOffset DateTime, int WeeekNumber)
        {
            return (Math.Floor(DateTime.Day / 7.0)) == WeeekNumber;
        }
        //public static void ConvertDatesToUtc<T>(T entity)
        //{
        //    var config = new ConfigurationBuilder()
        //        .SetBasePath(Directory.GetCurrentDirectory())
        //        .AddJsonFile("appsettings.json", optional: false)
        //        .Build();

        //    EProvider Provider = config.GetValue<string>("ConnectionStrings:DatabaseProvider") == "MySql" ? EProvider.MySql : EProvider.PostgreSQL;

        //    if (Provider != null && Provider==EProvider.PostgreSQL)
        //    {
        //        var props = typeof(T).GetProperties()
        //            .Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));
        //        foreach (var prop in props)
        //        {
        //            var value = prop.GetValue(entity) as DateTimeOffset?;
        //            if (value.HasValue)
        //            {
        //                prop.SetValue(entity, value.Value.ToUniversalTime());
        //            }
        //        }
        //    }
        //}
    }
}
