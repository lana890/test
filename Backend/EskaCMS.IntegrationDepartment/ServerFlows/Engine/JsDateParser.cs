namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// A port of V8's <c>Date.parse</c> (src/date/dateparser-inl.h), which is what
    /// <c>new Date("…")</c> runs in Chrome: the ES5 ISO format first, then V8's legacy
    /// parser for everything else ("2026-08-01 10:00", "08/01/2026", "Aug 1, 2026",
    /// Date.prototype.toString output …). Date-only ISO strings are UTC; date-time
    /// strings without an offset are local time (the context zone).
    /// </summary>
    internal static class JsDateParser
    {
        private const int None = int.MinValue;
        private const int MaxSignificantDigits = 9;

        /// <summary>Epoch milliseconds, or NaN when V8 would answer NaN.</summary>
        internal static double Parse(string text, TimeZoneInfo zone)
        {
            var scanner = new Scanner(text);
            var day = new DayComposer();
            var time = new TimeComposer();
            var tz = new TimeZoneComposer();

            Token next = ParseEs5(scanner, day, time, tz);
            if (next.Tag == TokenTag.Invalid) return double.NaN;
            bool hasReadNumber = !day.IsEmpty;

            for (Token token = next; token.Tag != TokenTag.EndOfInput; token = scanner.Next())
            {
                if (token.Tag == TokenTag.Number)
                {
                    hasReadNumber = true;
                    int n = token.Value;
                    if (scanner.SkipSymbol(':'))
                    {
                        if (scanner.SkipSymbol(':'))
                        {
                            // n + "::"
                            if (!time.IsEmpty) return double.NaN;
                            time.Add(n);
                            time.Add(0);
                        }
                        else
                        {
                            // n + ":"
                            if (!time.Add(n)) return double.NaN;
                            if (scanner.Peek().IsSymbol('.')) scanner.Next();
                        }
                    }
                    else
                    {
                        // V8 consumes a following '.' even when it then turns out not
                        // to start milliseconds ("1.2.2026" reads as 1, 2, 2026).
                        bool skippedDot = scanner.SkipSymbol('.');
                        if (skippedDot && time.IsExpecting(n))
                        {
                            time.Add(n);
                            if (scanner.Peek().Tag != TokenTag.Number) return double.NaN;
                            int ms = ReadMilliseconds(scanner.Next());
                            if (ms < 0) return double.NaN;
                            time.AddFinal(ms);
                        }
                        else if (tz.IsExpecting(n))
                        {
                            tz.Minute = n;
                        }
                        else if (time.IsExpecting(n))
                        {
                            time.AddFinal(n);
                            // Require end, white space, "Z", "+" or "-" immediately after finalizing time.
                            Token peek = scanner.Peek();
                            if (peek.Tag != TokenTag.EndOfInput && peek.Tag != TokenTag.WhiteSpace && !peek.IsKeywordZ && !peek.IsAsciiSign) return double.NaN;
                        }
                        else
                        {
                            if (!day.Add(n)) return double.NaN;
                            scanner.SkipSymbol('-');
                        }
                    }
                }
                else if (token.Tag == TokenTag.Keyword)
                {
                    if (token.Keyword == KeywordType.AmPm && !time.IsEmpty)
                    {
                        time.HourOffset = token.Value;
                    }
                    else if (token.Keyword == KeywordType.MonthName)
                    {
                        day.NamedMonth = token.Value;
                        scanner.SkipSymbol('-');
                    }
                    else if (token.Keyword == KeywordType.TimeZoneName && hasReadNumber)
                    {
                        tz.Set(token.Value);
                    }
                    else
                    {
                        // Garbage words are illegal if a number has been read.
                        if (hasReadNumber) return double.NaN;
                        // The first number has to be separated from garbage words by whitespace or other separators.
                        if (scanner.Peek().Tag == TokenTag.Number) return double.NaN;
                    }
                }
                else if (token.IsAsciiSign && (tz.IsUtc || !time.IsEmpty))
                {
                    // Parse UTC offset (only after UTC or time).
                    tz.Sign = token.AsciiSign;
                    int n = 0;
                    int length = 0;
                    if (scanner.Peek().Tag == TokenTag.Number)
                    {
                        Token number = scanner.Next();
                        length = number.Length;
                        n = number.Value;
                    }
                    hasReadNumber = true;
                    if (scanner.Peek().IsSymbol(':'))
                    {
                        tz.Hour = n;
                        tz.Minute = None;
                    }
                    else if (length == 2 || length == 1)
                    {
                        tz.Hour = n;
                        tz.Minute = 0;
                    }
                    else if (length == 4 || length == 3)
                    {
                        tz.Hour = n / 100;
                        tz.Minute = n % 100;
                    }
                    else
                    {
                        return double.NaN;
                    }
                }
                else if ((token.IsAsciiSign || token.IsSymbol(')')) && hasReadNumber)
                {
                    // Extra sign or ')' is illegal if a number has been read.
                    return double.NaN;
                }
                // Ignore other characters and whitespace.
            }

            if (!day.Write(out int year, out int month0, out int dayOfMonth)) return double.NaN;
            if (!time.Write(out int hour, out int minute, out int second, out int millisecond)) return double.NaN;
            double? offsetSeconds = tz.Write(out bool tzOk);
            if (!tzOk) return double.NaN;

            double date = JsDate.MakeDate(JsDate.MakeDay(year, month0, dayOfMonth), JsDate.MakeTime(hour, minute, second, millisecond));
            if (offsetSeconds == null)
            {
                long? utc = JsDate.LocalToUtc(date, zone);
                return utc.HasValue ? utc.Value : double.NaN;
            }
            return date - offsetSeconds.Value * 1000d;
        }

        private static Token ParseEs5(Scanner scanner, DayComposer day, TimeComposer time, TimeZoneComposer tz)
        {
            // Mandatory date: [('-'|'+')yy]yyyy[':'MM[':'DD]]
            if (scanner.Peek().IsAsciiSign)
            {
                Token signToken = scanner.Next();
                if (!scanner.Peek().IsFixedLengthNumber(6)) return signToken;
                int sign = signToken.AsciiSign;
                int year = scanner.Next().Value;
                if (sign < 0 && year == 0) return signToken;
                day.Add(sign * year);
            }
            else if (scanner.Peek().IsFixedLengthNumber(4))
            {
                day.Add(scanner.Next().Value);
            }
            else
            {
                return scanner.Next();
            }
            if (scanner.SkipSymbol('-'))
            {
                if (!scanner.Peek().IsFixedLengthNumber(2) || !DayComposer.IsMonth(scanner.Peek().Value)) return scanner.Next();
                day.Add(scanner.Next().Value);
                if (scanner.SkipSymbol('-'))
                {
                    if (!scanner.Peek().IsFixedLengthNumber(2) || !DayComposer.IsDay(scanner.Peek().Value)) return scanner.Next();
                    day.Add(scanner.Next().Value);
                }
            }
            // Optional time: 'T'HH':'mm[':'ss['.'sss]]Z
            if (!scanner.Peek().IsKeywordType(KeywordType.TimeSeparator))
            {
                if (scanner.Peek().Tag != TokenTag.EndOfInput) return scanner.Next();
            }
            else
            {
                scanner.Next();
                if (!scanner.Peek().IsFixedLengthNumber(2) || !Between(scanner.Peek().Value, 0, 24)) return Token.Invalid;
                // Allow 24:00[:00[.000]], but no other time starting with 24.
                bool hourIs24 = scanner.Peek().Value == 24;
                time.Add(scanner.Next().Value);
                if (!scanner.SkipSymbol(':')) return Token.Invalid;
                if (!scanner.Peek().IsFixedLengthNumber(2) || !TimeComposer.IsMinute(scanner.Peek().Value) || (hourIs24 && scanner.Peek().Value > 0)) return Token.Invalid;
                time.Add(scanner.Next().Value);
                if (scanner.SkipSymbol(':'))
                {
                    if (!scanner.Peek().IsFixedLengthNumber(2) || !TimeComposer.IsSecond(scanner.Peek().Value) || (hourIs24 && scanner.Peek().Value > 0)) return Token.Invalid;
                    time.Add(scanner.Next().Value);
                    if (scanner.SkipSymbol('.'))
                    {
                        if (scanner.Peek().Tag != TokenTag.Number || (hourIs24 && scanner.Peek().Value > 0)) return Token.Invalid;
                        // Allow more or less than the mandated three digits.
                        time.Add(ReadMilliseconds(scanner.Next()));
                    }
                }
                // Optional timezone designation: 'Z' | ('+'|'-')hh':'mm
                if (scanner.Peek().IsKeywordZ)
                {
                    scanner.Next();
                    tz.Set(0);
                }
                else if (scanner.Peek().IsSymbol('+') || scanner.Peek().IsSymbol('-'))
                {
                    tz.Sign = scanner.Next().Symbol == '+' ? 1 : -1;
                    if (scanner.Peek().IsFixedLengthNumber(4))
                    {
                        // hhmm extension syntax.
                        int hourMin = scanner.Next().Value;
                        int hour = hourMin / 100;
                        int min = hourMin % 100;
                        if (!TimeComposer.IsHour(hour) || !TimeComposer.IsMinute(min)) return Token.Invalid;
                        tz.Hour = hour;
                        tz.Minute = min;
                    }
                    else
                    {
                        if (!scanner.Peek().IsFixedLengthNumber(2) || !TimeComposer.IsHour(scanner.Peek().Value)) return Token.Invalid;
                        tz.Hour = scanner.Next().Value;
                        if (!scanner.SkipSymbol(':')) return Token.Invalid;
                        if (!scanner.Peek().IsFixedLengthNumber(2) || !TimeComposer.IsMinute(scanner.Peek().Value)) return Token.Invalid;
                        tz.Minute = scanner.Next().Value;
                    }
                }
                if (scanner.Peek().Tag != TokenTag.EndOfInput) return Token.Invalid;
            }
            // "When the time zone offset is absent, date-only forms are interpreted
            // as a UTC time and date-time forms are interpreted as a local time."
            if (tz.IsEmpty && time.IsEmpty) tz.Set(0);
            day.IsIsoDate = true;
            return Token.EndOfInput;
        }

        private static int ReadMilliseconds(Token token)
        {
            // The first three significant digits of the numeral, recovered from the
            // value and the digit count (which includes leading zeros).
            int number = token.Value;
            int length = token.Length;
            if (length < 3)
            {
                if (length == 1) number *= 100;
                else if (length == 2) number *= 10;
            }
            else if (length > 3)
            {
                if (length > MaxSignificantDigits) length = MaxSignificantDigits;
                int factor = 1;
                do
                {
                    factor *= 10;
                    length--;
                }
                while (length > 3);
                number /= factor;
            }
            return number;
        }

        private static bool Between(int x, int lo, int hi) => x >= lo && x <= hi;

        private enum TokenTag
        {
            Invalid,
            Unknown,
            WhiteSpace,
            Number,
            Symbol,
            EndOfInput,
            Keyword,
        }

        private enum KeywordType
        {
            Invalid,
            MonthName,
            TimeZoneName,
            TimeSeparator,
            AmPm,
        }

        private readonly struct Token
        {
            private Token(TokenTag tag, int value, int length, char symbol, KeywordType keyword)
            {
                Tag = tag;
                Value = value;
                Length = length;
                Symbol = symbol;
                Keyword = keyword;
            }

            internal static Token Invalid => new Token(TokenTag.Invalid, 0, 0, '\0', KeywordType.Invalid);

            internal static Token EndOfInput => new Token(TokenTag.EndOfInput, 0, 0, '\0', KeywordType.Invalid);

            internal static Token Unknown => new Token(TokenTag.Unknown, 0, 0, '\0', KeywordType.Invalid);

            internal TokenTag Tag { get; }

            internal int Value { get; }

            internal int Length { get; }

            internal char Symbol { get; }

            internal KeywordType Keyword { get; }

            internal bool IsSymbol(char c) => Tag == TokenTag.Symbol && Symbol == c;

            internal bool IsAsciiSign => Tag == TokenTag.Symbol && (Symbol == '+' || Symbol == '-');

            internal int AsciiSign => Symbol == '-' ? -1 : 1;

            internal bool IsFixedLengthNumber(int n) => Tag == TokenTag.Number && Length == n;

            internal bool IsKeywordType(KeywordType type) => Tag == TokenTag.Keyword && Keyword == type;

            internal bool IsKeywordZ => Tag == TokenTag.Keyword && Keyword == KeywordType.TimeZoneName && Length == 1 && Value == 0;

            internal static Token Number(int value, int length) => new Token(TokenTag.Number, value, length, '\0', KeywordType.Invalid);

            internal static Token Sym(char c) => new Token(TokenTag.Symbol, 0, 1, c, KeywordType.Invalid);

            internal static Token Space(int length) => new Token(TokenTag.WhiteSpace, 0, length, '\0', KeywordType.Invalid);

            internal static Token Word(KeywordType type, int value, int length) => new Token(TokenTag.Keyword, value, length, '\0', type);
        }

        private sealed class Scanner
        {
            private readonly string _s;
            private int _pos;
            private Token _next;

            internal Scanner(string s)
            {
                _s = s;
                _next = Scan();
            }

            internal Token Next()
            {
                Token result = _next;
                _next = Scan();
                return result;
            }

            internal Token Peek() => _next;

            internal bool SkipSymbol(char c)
            {
                if (!_next.IsSymbol(c)) return false;
                Next();
                return true;
            }

            private char Ch => _pos < _s.Length ? _s[_pos] : '\0';

            private bool AtEnd => _pos >= _s.Length;

            private Token Scan()
            {
                int pre = _pos;
                if (AtEnd) return Token.EndOfInput;
                char c = Ch;
                if (c >= '0' && c <= '9')
                {
                    int n = 0;
                    int i = 0;
                    while (!AtEnd && Ch == '0') _pos++;
                    while (!AtEnd && Ch >= '0' && Ch <= '9')
                    {
                        if (i < MaxSignificantDigits) n = n * 10 + (Ch - '0');
                        i++;
                        _pos++;
                    }
                    return Token.Number(n, _pos - pre);
                }
                if (c == ':' || c == '-' || c == '+' || c == '.' || c == ')')
                {
                    _pos++;
                    return Token.Sym(c);
                }
                if (c >= 'A' && !JsStrings.IsWhiteSpace(c))
                {
                    // ReadWord: lower-cased 3-char prefix, full length counted.
                    char p0 = '\0', p1 = '\0', p2 = '\0';
                    int len = 0;
                    while (!AtEnd && Ch >= 'A' && !JsStrings.IsWhiteSpace(Ch))
                    {
                        char lower = (char)(Ch | 0x20);
                        if (len == 0) p0 = lower;
                        else if (len == 1) p1 = lower;
                        else if (len == 2) p2 = lower;
                        len++;
                        _pos++;
                    }
                    return Lookup(p0, p1, p2, len);
                }
                if (JsStrings.IsWhiteSpace(c))
                {
                    _pos++;
                    return Token.Space(1);
                }
                if (c == '(')
                {
                    int balance = 0;
                    do
                    {
                        if (Ch == ')') balance--;
                        else if (Ch == '(') balance++;
                        _pos++;
                    }
                    while (balance > 0 && !AtEnd);
                    return Token.Unknown;
                }
                _pos++;
                return Token.Unknown;
            }

            private static readonly (string Prefix, KeywordType Type, int Value)[] Keywords =
            {
                ("jan", KeywordType.MonthName, 1), ("feb", KeywordType.MonthName, 2), ("mar", KeywordType.MonthName, 3),
                ("apr", KeywordType.MonthName, 4), ("may", KeywordType.MonthName, 5), ("jun", KeywordType.MonthName, 6),
                ("jul", KeywordType.MonthName, 7), ("aug", KeywordType.MonthName, 8), ("sep", KeywordType.MonthName, 9),
                ("oct", KeywordType.MonthName, 10), ("nov", KeywordType.MonthName, 11), ("dec", KeywordType.MonthName, 12),
                ("am\0", KeywordType.AmPm, 0), ("pm\0", KeywordType.AmPm, 12),
                ("ut\0", KeywordType.TimeZoneName, 0), ("utc", KeywordType.TimeZoneName, 0), ("z\0\0", KeywordType.TimeZoneName, 0),
                ("gmt", KeywordType.TimeZoneName, 0), ("cdt", KeywordType.TimeZoneName, -5), ("cst", KeywordType.TimeZoneName, -6),
                ("edt", KeywordType.TimeZoneName, -4), ("est", KeywordType.TimeZoneName, -5), ("mdt", KeywordType.TimeZoneName, -6),
                ("mst", KeywordType.TimeZoneName, -7), ("pdt", KeywordType.TimeZoneName, -7), ("pst", KeywordType.TimeZoneName, -8),
                ("t\0\0", KeywordType.TimeSeparator, 0),
            };

            private static Token Lookup(char p0, char p1, char p2, int length)
            {
                foreach (var (prefix, type, value) in Keywords)
                {
                    if (prefix[0] == p0 && prefix[1] == p1 && prefix[2] == p2 && (length <= 3 || type == KeywordType.MonthName))
                    {
                        return Token.Word(type, value, length);
                    }
                }
                return Token.Word(KeywordType.Invalid, 0, length);
            }
        }

        private sealed class DayComposer
        {
            private readonly int[] _comp = new int[3];
            private int _index;

            internal int NamedMonth { get; set; } = None;

            internal bool IsIsoDate { get; set; }

            internal bool IsEmpty => _index == 0;

            internal static bool IsMonth(int x) => Between(x, 1, 12);

            internal static bool IsDay(int x) => Between(x, 1, 31);

            internal bool Add(int n)
            {
                if (_index >= 3) return false;
                _comp[_index++] = n;
                return true;
            }

            internal bool Write(out int year, out int month0, out int day)
            {
                year = 0;
                month0 = 0;
                day = 0;
                if (_index < 1) return false;
                // Day and month default to 1.
                while (_index < 3) _comp[_index++] = 1;

                int y = 0; // Default year is 0 (=> 2000) for KJS compatibility.
                int m;
                int d;
                if (NamedMonth == None)
                {
                    if (IsIsoDate || (_index == 3 && !IsDay(_comp[0])))
                    {
                        y = _comp[0];
                        m = _comp[1];
                        d = _comp[2];
                    }
                    else
                    {
                        m = _comp[0];
                        d = _comp[1];
                        if (_index == 3) y = _comp[2];
                    }
                }
                else
                {
                    m = NamedMonth;
                    if (!IsDay(_comp[0]))
                    {
                        y = _comp[0];
                        d = _comp[1];
                    }
                    else
                    {
                        d = _comp[0];
                        y = _comp[1];
                    }
                }
                if (!IsIsoDate)
                {
                    if (Between(y, 0, 49)) y += 2000;
                    else if (Between(y, 50, 99)) y += 1900;
                }
                if (!IsMonth(m) || !IsDay(d)) return false;
                year = y;
                month0 = m - 1;
                day = d;
                return true;
            }
        }

        private sealed class TimeComposer
        {
            private readonly int[] _comp = new int[4];
            private int _index;

            internal int HourOffset { get; set; } = None;

            internal bool IsEmpty => _index == 0;

            internal static bool IsMinute(int x) => Between(x, 0, 59);

            internal static bool IsHour(int x) => Between(x, 0, 23);

            internal static bool IsSecond(int x) => Between(x, 0, 59);

            private static bool IsHour12(int x) => Between(x, 0, 12);

            private static bool IsMillisecond(int x) => Between(x, 0, 999);

            internal bool IsExpecting(int n) =>
                (_index == 1 && IsMinute(n)) || (_index == 2 && IsSecond(n)) || (_index == 3 && IsMillisecond(n));

            internal bool Add(int n)
            {
                if (_index >= 4) return false;
                _comp[_index++] = n;
                return true;
            }

            internal bool AddFinal(int n)
            {
                if (!Add(n)) return false;
                while (_index < 4) _comp[_index++] = 0;
                return true;
            }

            internal bool Write(out int hour, out int minute, out int second, out int millisecond)
            {
                while (_index < 4) _comp[_index++] = 0;
                hour = _comp[0];
                minute = _comp[1];
                second = _comp[2];
                millisecond = _comp[3];
                if (HourOffset != None)
                {
                    if (!IsHour12(hour)) return false;
                    hour %= 12;
                    hour += HourOffset;
                }
                if (!IsHour(hour) || !IsMinute(minute) || !IsSecond(second) || !IsMillisecond(millisecond))
                {
                    // A 24th hour is allowed if minutes, seconds, and milliseconds are 0.
                    if (hour != 24 || minute != 0 || second != 0 || millisecond != 0) return false;
                }
                return true;
            }
        }

        private sealed class TimeZoneComposer
        {
            internal int Sign { get; set; } = None;

            internal int Hour { get; set; } = None;

            internal int Minute { get; set; } = None;

            internal bool IsEmpty => Hour == None;

            internal bool IsUtc => Hour == 0 && Minute == 0;

            internal void Set(int offsetInHours)
            {
                Sign = offsetInHours < 0 ? -1 : 1;
                Hour = offsetInHours * Sign;
                Minute = 0;
            }

            internal bool IsExpecting(int n) => Hour != None && Minute == None && TimeComposer.IsMinute(n);

            /// <summary>Offset in seconds, or null for local time.</summary>
            internal double? Write(out bool ok)
            {
                ok = true;
                if (Sign == None) return null;
                int hour = Hour == None ? 0 : Hour;
                int minute = Minute == None ? 0 : Minute;
                long total = hour * 3600L + minute * 60L;
                if (total > int.MaxValue / 2)
                {
                    ok = false;
                    return null;
                }
                return Sign < 0 ? -total : total;
            }
        }
    }
}
