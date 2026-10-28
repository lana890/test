namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// The Umm al-Qura calendar exactly as the browser computes it.
    /// </summary>
    /// <remarks>
    /// The TS reads Hijri dates through <c>Intl.DateTimeFormat('en-u-ca-islamic-umalqura')</c>,
    /// i.e. ICU's Umm al-Qura table. .NET's <see cref="System.Globalization.UmAlQuraCalendar"/>
    /// carries a different historical table and disagrees with ICU before ~1420 AH
    /// (1990-12-25 is 07/06/1411 in ICU and on ummulqura.org.sa, 08/06/1411 in .NET) —
    /// which matters for birth dates. This table was generated from ICU 78.2
    /// (Node 22) by walking every day and recording each 1 Muharram and month length.
    /// It covers ICU's own table range, AH 1300–1600 (1882-11-12 … 2174-11-25);
    /// outside it ICU falls back to the arithmetic (civil) Islamic calendar, ported below
    /// the same way.
    /// </remarks>
    internal static class UmAlQura
    {
        private const int FirstYear = 1300;

        /// <summary>Pairs of (days since 1970-01-01 of 1 Muharram, month mask: bit i set = month i+1 has 30 days).</summary>
        private static readonly int[] Table =
        {
            -31826, 0x555, -31472, 0x2AB, -31118, 0x937, -30763, 0x2B6, -30409, 0x576, -30054, 0x36C,
            -29700, 0xB55, -29345, 0xAAA, -28991, 0x956, -28637, 0x49E, -28283, 0x95D, -27928, 0x2BA,
            -27574, 0x5B5, -27219, 0x3AA, -26865, 0xB4B, -26510, 0xA96, -26156, 0x52E, -25802, 0x2AD,
            -25448, 0x56D, -25093, 0xB5A, -24738, 0x752, -24384, 0xF25, -24029, 0xE8A, -23675, 0xD16,
            -23321, 0xA56, -22967, 0xAB5, -22612, 0x6B4, -22258, 0xDA9, -21903, 0xB92, -21549, 0xB25,
            -21195, 0x64B, -20841, 0xA9B, -20486, 0x35A, -20132, 0x6D9, -19777, 0x5D4, -19423, 0xDA5,
            -19068, 0xD4A, -18714, 0xA95, -18360, 0x536, -18006, 0x975, -17651, 0x2F4, -17297, 0x6E9,
            -16942, 0x6D4, -16588, 0x6A9, -16234, 0x535, -15880, 0x25D, -15526, 0x4BD, -15171, 0x9BA,
            -14816, 0x3B4, -14462, 0xB69, -14107, 0xB2A, -13753, 0xA55, -13399, 0x4AD, -13045, 0xA5D,
            -12690, 0x2DA, -12336, 0x6D9, -11981, 0xEAA, -11626, 0xE94, -11272, 0xD2A, -10918, 0xC56,
            -10564, 0x4AE, -10210, 0xA6D, -9855, 0x56A, -9501, 0xD55, -9146, 0xD4A, -8792, 0xA93,
            -8438, 0x52B, -8084, 0xA5B, -7729, 0x53A, -7375, 0x6B5, -7020, 0xEA9, -6665, 0xD52,
            -6311, 0xD29, -5957, 0xA55, -5603, 0x4AD, -5249, 0x56D, -4894, 0xAEA, -4539, 0x6E4,
            -4185, 0xED1, -3830, 0xDA2, -3476, 0xAAA, -3122, 0x95A, -2768, 0x2DA, -2414, 0x5B9,
            -2059, 0xBB2, -1704, 0x764, -1350, 0x6C9, -996, 0x555, -642, 0x2AB, -288, 0x4DB,
            67, 0xABA, 422, 0x5B4, 776, 0xDA9, 1131, 0xD52, 1485, 0xAA5, 1839, 0x92D,
            2193, 0x26D, 2547, 0x8ED, 2902, 0x2DA, 3256, 0xAD5, 3611, 0xAA5, 3965, 0xA4B,
            4319, 0x497, 4673, 0x937, 5028, 0x2B6, 5382, 0x975, 5737, 0xD69, 6092, 0xD52,
            6446, 0xC95, 6800, 0x92B, 7154, 0x25B, 7508, 0x4DB, 7863, 0x9D5, 8218, 0x5D2,
            8572, 0xDA5, 8927, 0xD4A, 9281, 0xA95, 9635, 0x54D, 9989, 0xAAD, 10344, 0x3AA,
            10698, 0xBD2, 11053, 0xBC4, 11407, 0xB89, 11761, 0xA95, 12115, 0x52D, 12469, 0x5AD,
            12824, 0xB6A, 13179, 0x6D4, 13533, 0xDC9, 13888, 0xD92, 14242, 0xAA6, 14596, 0x956,
            14950, 0x2AE, 15304, 0x56D, 15659, 0x36A, 16013, 0xB55, 16368, 0xAAA, 16722, 0x94D,
            17076, 0x49D, 17430, 0x95D, 17785, 0x2BA, 18139, 0x5B5, 18494, 0x5AA, 18848, 0xD55,
            19203, 0xA9A, 19557, 0x92E, 19911, 0x26E, 20265, 0x55D, 20620, 0xADA, 20975, 0x6D4,
            21329, 0x6A5, 21683, 0xB27, 22038, 0xA4D, 22392, 0x4AD, 22746, 0x56D, 23101, 0xB5A,
            23456, 0x754, 23810, 0xF49, 24165, 0xE92, 24519, 0xD26, 24873, 0xA56, 25227, 0x356,
            25581, 0x6B5, 25936, 0xBAA, 26291, 0xB92, 26645, 0xB25, 26999, 0x68B, 27353, 0xA9B,
            27708, 0x55A, 28062, 0xADA, 28417, 0x5B4, 28771, 0xDA9, 29126, 0xB52, 29480, 0xA9A,
            29834, 0x536, 30188, 0x276, 30542, 0x575, 30897, 0xAF2, 31252, 0x6D4, 31606, 0x6A9,
            31960, 0x555, 32314, 0x2AD, 32668, 0x4BD, 33023, 0x9BA, 33378, 0x574, 33732, 0xB69,
            34087, 0xB52, 34441, 0xA95, 34795, 0x52D, 35149, 0xA5D, 35504, 0x4DA, 35858, 0xAD9,
            36213, 0x6B2, 36567, 0xE95, 36922, 0xE2A, 37276, 0xC96, 37630, 0x92E, 37984, 0xAAD,
            38339, 0x56A, 38693, 0xD65, 39048, 0xD4A, 39402, 0xD15, 39756, 0x62B, 40110, 0xC5B,
            40465, 0x53A, 40819, 0x6B5, 41174, 0xDB2, 41529, 0xD64, 41883, 0xD29, 42237, 0xA55,
            42591, 0x4AD, 42945, 0x96D, 43300, 0xAEA, 43655, 0x6E8, 44009, 0xED1, 44364, 0xDA4,
            44718, 0xD4A, 45072, 0xA6A, 45426, 0x2DA, 45780, 0x5B9, 46135, 0xB72, 46490, 0xB68,
            46844, 0x6D1, 47198, 0x655, 47552, 0x4AB, 47906, 0x95B, 48261, 0x2BA, 48615, 0x5B5,
            48970, 0xDA9, 49325, 0xD52, 49679, 0xCA6, 50033, 0x94E, 50387, 0x46E, 50741, 0x95D,
            51096, 0x4DA, 51450, 0xAD5, 51805, 0xAAA, 52159, 0xA4D, 52513, 0x49B, 52867, 0x937,
            53222, 0x4B6, 53576, 0x975, 53931, 0xD6A, 54286, 0xD52, 54640, 0xAA5, 54994, 0x94B,
            55348, 0x2AB, 55702, 0x55B, 56057, 0xAD9, 56412, 0x5D2, 56766, 0xDC5, 57121, 0xD92,
            57475, 0xB25, 57829, 0x555, 58183, 0xAB5, 58538, 0x5B4, 58892, 0xBA9, 59247, 0x7A2,
            59601, 0x745, 59955, 0x593, 60309, 0xAAB, 60664, 0x4D6, 61018, 0x9D6, 61373, 0x5D2,
            61727, 0xBA5, 62082, 0xB4A, 62436, 0xA95, 62790, 0x4AD, 63144, 0x15D, 63498, 0x2DD,
            63853, 0x9DA, 64208, 0x5B4, 64562, 0x5A9, 64916, 0x52D, 65270, 0x25B, 65624, 0x8B7,
            65979, 0x176, 66333, 0x56D, 66688, 0xB6A, 67043, 0xACA, 67397, 0xA96, 67751, 0x52B,
            68105, 0x15B, 68459, 0x2BB, 68814, 0x5B6, 69169, 0xDAA, 69524, 0xB94, 69878, 0xD46,
            70232, 0xA8D, 70586, 0x52D, 70940, 0xA9D, 71295, 0x55A, 71649, 0x755, 72004, 0x749,
            72358, 0xF13, 72713, 0xE4A, 73067, 0xA96, 73421, 0x556, 73775, 0x6B5, 74130, 0xBAA,
            74485, 0xB94,
        };

        private static readonly int YearCount = Table.Length / 2;

        private const int LastYear = FirstYear + 300;

        /// <summary>Days since 1970-01-01 plus this = days since ICU's Islamic civil epoch (Julian day 1948440).</summary>
        private const long EpochOffset = 492148;

        /// <summary>Days since 1970-01-01 of the first day after the table (1 Muharram 1601).</summary>
        private static readonly long EndDay = Table[(YearCount - 1) * 2] + TableYearLength(Table[(YearCount - 1) * 2 + 1]);

        private static int TableYearLength(int mask)
        {
            int days = 0;
            for (int b = 0; b < 12; b++) days += ((mask >> b) & 1) == 1 ? 30 : 29;
            return days;
        }

        /// <summary>ICU ClockMath::floorDivide.</summary>
        private static long FloorDivide(long numerator, long denominator) =>
            numerator >= 0 ? numerator / denominator : ((numerator + 1) / denominator) - 1;

        private static bool InTable(long year) => year >= FirstYear && year <= LastYear;

        /// <summary>ICU yearStart: the table inside AH 1300-1600, the arithmetic (civil) calendar outside it. Islamic-epoch days.</summary>
        private static long YearStart(long year) =>
            InTable(year) ? Table[(year - FirstYear) * 2] + EpochOffset : 354L * (year - 1) + FloorDivide(3 + 11L * year, 30);

        private static int MonthLength(long year, int month0) =>
            InTable(year) ? (((Table[(year - FirstYear) * 2 + 1] >> month0) & 1) == 1 ? 30 : 29) : 29 + (month0 + 1) % 2;

        private static int YearLength(long year) =>
            InTable(year) ? TableYearLength(Table[(year - FirstYear) * 2 + 1]) : 354 + ((14 + 11 * year) % 30 < 11 ? 1 : 0);

        private static long MonthStart(long year, int month0)
        {
            long start = YearStart(year);
            for (int m = 0; m < month0; m++) start += MonthLength(year, m);
            return start;
        }

        /// <summary>
        /// The Hijri date of a civil day (days since 1970-01-01), as ICU's
        /// IslamicUmalquraCalendar::handleComputeFields computes it: the table for AH
        /// 1300-1600, the civil formula before it, and ICU's year-by-year walk after it.
        /// False when the result has no positive year/month/day (the TS then falls back).
        /// </summary>
        internal static bool TryFromDay(long day, out int year, out int month, out int dayOfMonth)
        {
            year = month = dayOfMonth = 0;
            long days = day + EpochOffset;
            long y;
            int m0;
            if (days < YearStart(FirstYear))
            {
                y = FloorDivide(30 * days + 10646, 10631);
                m0 = (int)Math.Ceiling((days - 29 - YearStart(y)) / 29.5);
                if (m0 > 11) m0 = 11;
            }
            else if (day < EndDay)
            {
                int lo = 0;
                int hi = YearCount - 1;
                while (lo < hi)
                {
                    int mid = (lo + hi + 1) / 2;
                    if (Table[mid * 2] <= day) lo = mid;
                    else hi = mid - 1;
                }
                y = FirstYear + lo;
                long offset = days - YearStart(y);
                m0 = 0;
                while (m0 < 11 && offset >= MonthLength(y, m0))
                {
                    offset -= MonthLength(y, m0);
                    m0++;
                }
            }
            else
            {
                // ICU walks the years forward; past the table they are civil years.
                y = LastYear;
                m0 = 0;
                long d = 1;
                while (d > 0)
                {
                    y++;
                    d = days - YearStart(y) + 1;
                    int length = YearLength(y);
                    if (d == length)
                    {
                        m0 = 11;
                        break;
                    }
                    if (d < length)
                    {
                        int monthLength = MonthLength(y, 0);
                        m0 = 0;
                        while (d > monthLength)
                        {
                            d -= monthLength;
                            m0++;
                            monthLength = MonthLength(y, m0);
                        }
                        break;
                    }
                }
            }
            long dom = days - MonthStart(y, m0) + 1;
            if (y < 1 || y > int.MaxValue || dom < 1) return false;
            year = (int)y;
            month = m0 + 1;
            dayOfMonth = (int)dom;
            return true;
        }

        /// <summary>The civil day (days since 1970-01-01) of a Hijri date inside the table, or false if it does not exist there.</summary>
        internal static bool TryToDay(int year, int month, int dayOfMonth, out long day)
        {
            day = 0;
            if (!InTable(year) || month < 1 || month > 12 || dayOfMonth < 1) return false;
            if (dayOfMonth > MonthLength(year, month - 1)) return false;
            day = MonthStart(year, month - 1) - EpochOffset + dayOfMonth - 1;
            return true;
        }
    }
}
