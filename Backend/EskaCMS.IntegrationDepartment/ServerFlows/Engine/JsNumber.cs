using System.Globalization;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// JavaScript Number semantics: Number::toString(10), StringToNumber (what
    /// <c>Number("…")</c> does), ToIntegerOrInfinity and <c>Math.round</c>.
    /// </summary>
    internal static class JsNumber
    {
        /// <summary>Number::toString(10): shortest round-trip digits laid out the JS way.</summary>
        internal static string ToJsString(double d)
        {
            if (double.IsNaN(d)) return "NaN";
            if (d == 0) return "0"; // covers -0, which JS also prints as "0"
            if (double.IsPositiveInfinity(d)) return "Infinity";
            if (double.IsNegativeInfinity(d)) return "-Infinity";

            string sign = d < 0 ? "-" : string.Empty;
            // .NET Core 3.0+ "R" is the shortest string that round-trips, which is
            // exactly the digit string ES requires; only the layout differs.
            string r = Math.Abs(d).ToString("R", CultureInfo.InvariantCulture);
            int ePos = r.IndexOfAny(ExponentChars);
            string mantissa = ePos >= 0 ? r.Substring(0, ePos) : r;
            int exponent = ePos >= 0 ? int.Parse(r.Substring(ePos + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture) : 0;
            int dot = mantissa.IndexOf('.');
            string digits = dot >= 0 ? mantissa.Remove(dot, 1) : mantissa;
            int intLength = dot >= 0 ? dot : mantissa.Length;

            int lead = 0;
            while (lead < digits.Length - 1 && digits[lead] == '0') lead++;
            digits = digits.Substring(lead);
            intLength -= lead;
            digits = digits.TrimEnd('0');
            if (digits.Length == 0) digits = "0";

            int k = digits.Length;
            int n = intLength + exponent; // value = 0.digits x 10^n

            if (k <= n && n <= 21) return sign + digits + new string('0', n - k);
            if (0 < n && n <= 21) return sign + digits.Substring(0, n) + "." + digits.Substring(n);
            if (-6 < n && n <= 0) return sign + "0." + new string('0', -n) + digits;

            int e = n - 1;
            string exp = e >= 0 ? "+" + e.ToString(CultureInfo.InvariantCulture) : e.ToString(CultureInfo.InvariantCulture);
            return k == 1
                ? sign + digits + "e" + exp
                : sign + digits[0] + "." + digits.Substring(1) + "e" + exp;
        }

        private static readonly char[] ExponentChars = { 'E', 'e' };

        /// <summary>StringToNumber: what <c>Number(string)</c> returns.</summary>
        internal static double StringToNumber(string s)
        {
            string t = JsStrings.Trim(s);
            if (t.Length == 0) return 0;

            if (t.Length > 2 && t[0] == '0')
            {
                int radix = t[1] switch
                {
                    'x' or 'X' => 16,
                    'o' or 'O' => 8,
                    'b' or 'B' => 2,
                    _ => 0,
                };
                if (radix != 0) return ParseRadix(t, radix);
            }

            int i = 0;
            bool negative = false;
            if (t[0] == '+' || t[0] == '-')
            {
                negative = t[0] == '-';
                i = 1;
            }
            if (t.Length - i == 8 && string.CompareOrdinal(t, i, "Infinity", 0, 8) == 0)
            {
                return negative ? double.NegativeInfinity : double.PositiveInfinity;
            }

            int start = i;
            int intDigits = 0;
            int fracDigits = 0;
            while (i < t.Length && IsAsciiDigit(t[i]))
            {
                i++;
                intDigits++;
            }
            if (i < t.Length && t[i] == '.')
            {
                i++;
                while (i < t.Length && IsAsciiDigit(t[i]))
                {
                    i++;
                    fracDigits++;
                }
            }
            if (intDigits + fracDigits == 0) return double.NaN;
            if (i < t.Length && (t[i] == 'e' || t[i] == 'E'))
            {
                i++;
                if (i < t.Length && (t[i] == '+' || t[i] == '-')) i++;
                int expDigits = 0;
                while (i < t.Length && IsAsciiDigit(t[i]))
                {
                    i++;
                    expDigits++;
                }
                if (expDigits == 0) return double.NaN;
            }
            if (i != t.Length) return double.NaN;

            // "5." and ".5" are valid JS; give .NET the canonical spelling.
            string body = t.Substring(start);
            if (body[0] == '.') body = "0" + body;
            body = body.Replace(".e", ".0e").Replace(".E", ".0E");
            if (body[body.Length - 1] == '.') body += "0";
            double value = double.Parse(body, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
            return negative ? -value : value;
        }

        private static double ParseRadix(string t, int radix)
        {
            double value = 0;
            for (int i = 2; i < t.Length; i++)
            {
                int digit = HexValue(t[i]);
                if (digit < 0 || digit >= radix) return double.NaN;
                value = value * radix + digit;
            }
            return value;
        }

        private static int HexValue(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }

        internal static bool IsAsciiDigit(char c) => c >= '0' && c <= '9';

        /// <summary>ToIntegerOrInfinity: NaN → 0, ±∞ kept, otherwise truncated.</summary>
        internal static double ToIntegerOrInfinity(double d)
        {
            if (double.IsNaN(d)) return 0;
            if (double.IsInfinity(d)) return d;
            double t = Math.Truncate(d);
            return t == 0 ? 0 : t; // no -0
        }

        /// <summary>
        /// Math.round: halves go towards +∞ (−2.5 → −2). Written as floor + compare
        /// rather than floor(x + 0.5), which is wrong for 0.49999999999999994.
        /// </summary>
        internal static double Round(double x)
        {
            if (double.IsNaN(x) || double.IsInfinity(x)) return x;
            double r = Math.Floor(x);
            if (x - r >= 0.5) r += 1;
            return r;
        }

        /// <summary>ToUint32 as used by <c>array.length = n</c>; null when n is not a valid length.</summary>
        internal static uint? ToArrayLength(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d) || d < 0 || d > uint.MaxValue || Math.Truncate(d) != d) return null;
            return (uint)d;
        }
    }
}
