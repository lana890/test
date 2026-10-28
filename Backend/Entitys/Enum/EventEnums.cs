using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Enum
{
    public class EventEnums
    {
        public enum Importance : Int16

        {
            High = 1,
            Medium = 2,
            Low = 3
        }

        /// <summary>
        /// Repeat Types
        /// </summary>
        public enum RepeatType :Int16
        {
            OneTime = 3,
            Periodic = 4,
            Weekly = 5,
            Monthly = 6,
            InMonth = 7,
            Annual = 8
        }

        /// <summary>
        /// Period
        /// </summary>
        public enum PeriodType : Int16
        {
            Days = 1,
            Weeks = 2,
            Months = 3
        }
        //public enum EStatus : Int16

        //{
        //    Approved = 1,
        //    Cancelled = 2,
        //    Scheduled = 3,
        //    Daily = 4,
        //    Deleted=5
        //}
        /// <summary>
        /// Day
        /// </summary>
        public enum Day : int
        {
            Sunday = 14,
            Monday = 15,
            Tuesday = 16,
            Wednesday = 17,
            Thursday = 18,
            Friday = 19,
            Saturday = 20
        }
        static public int GetIndexOfDay(string DayName)
        {
            switch (DayName)
            {
                case "Sunday":
                    return (int)Day.Sunday;
                case "Monday":
                    return (int)Day.Monday;
                case "Tuesday":
                    return (int)Day.Tuesday;
                case "Wednesday":
                    return (int)Day.Wednesday;
                case "Thursday":
                    return (int)Day.Thursday;
                case "Friday":
                    return (int)Day.Friday;
                case "Saturday":
                    return (int)Day.Saturday;
            }
            return 0;
        }

        /// <summary>
        /// FirstLast
        /// </summary>
        public enum FirstLast : Int16
        {
            First = 1,
            Last = 2
        }

        /// <summary>
        /// Lockup
        /// </summary>
        public enum Lockup : Int16
        {
            Importance = 1,
            Periodic = 2,
            Day = 3,
            FirstLast = 4
        }

        public enum EventPlace : Int16
        {
            InHouse = 1,
            External = 2,
            Online = 3,
        }

    }
}
