using System.Text;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// JavaScript string semantics the ops rely on: the JS whitespace set (used by
    /// <c>trim</c> and <c>/\s/</c>), slice/pad/repeat index rules, split, case mapping,
    /// <c>GetSubstitution</c> for replace patterns and JSON string quoting.
    /// </summary>
    internal static class JsStrings
    {
        /// <summary>
        /// The longest string a server op may build with repeat/pad. V8 allows ~2^29
        /// characters and throws a RangeError above that; on a shared server that
        /// would be gigabytes, so the port throws earlier (the pipeline then keeps
        /// the previous value, exactly as it does for the browser's RangeError).
        /// </summary>
        internal const int MaxBuiltLength = 10_000_000;

        /// <summary>
        /// The JS <c>WhiteSpace</c> + <c>LineTerminator</c> set. It differs from
        /// <see cref="char.IsWhiteSpace(char)"/>: JS includes U+FEFF and excludes U+0085.
        /// </summary>
        internal static bool IsWhiteSpace(char c)
        {
            switch (c)
            {
                case '\t':
                case '\n':
                case '\v':
                case '\f':
                case '\r':
                case ' ':
                case '\u00A0':
                case '\u1680':
                case '\u2028':
                case '\u2029':
                case '\u202F':
                case '\u205F':
                case '\u3000':
                case '\uFEFF':
                    return true;
                default:
                    return c >= '\u2000' && c <= '\u200A';
            }
        }

        /// <summary>The body of a regex character class matching exactly JS <c>\s</c>.</summary>
        internal const string WhiteSpaceClassBody = @"\t\n\v\f\r \u00A0\u1680\u2000-\u200A\u2028\u2029\u202F\u205F\u3000\uFEFF";

        /// <summary>String.prototype.trim.</summary>
        internal static string Trim(string s)
        {
            int start = 0;
            int end = s.Length - 1;
            while (start <= end && IsWhiteSpace(s[start])) start++;
            while (end >= start && IsWhiteSpace(s[end])) end--;
            return start == 0 && end == s.Length - 1 ? s : s.Substring(start, end - start + 1);
        }

        /// <summary><c>s.replace(/\s+/g, replacement)</c> with the JS whitespace set.</summary>
        internal static string ReplaceWhiteSpaceRuns(string s, string replacement)
        {
            StringBuilder? sb = null;
            int i = 0;
            int copiedTo = 0;
            while (i < s.Length)
            {
                if (!IsWhiteSpace(s[i]))
                {
                    i++;
                    continue;
                }
                int runStart = i;
                while (i < s.Length && IsWhiteSpace(s[i])) i++;
                sb ??= new StringBuilder(s.Length);
                sb.Append(s, copiedTo, runStart - copiedTo).Append(replacement);
                copiedTo = i;
            }
            if (sb == null) return s;
            sb.Append(s, copiedTo, s.Length - copiedTo);
            return sb.ToString();
        }

        /// <summary>
        /// String.prototype.toUpperCase: invariant mapping plus the unconditional
        /// SpecialCasing expansions JS applies (ß → SS, ligatures). Context-sensitive
        /// and Greek iota-subscript rules are not reproduced.
        /// </summary>
        internal static string ToUpper(string s)
        {
            string upper = s.ToUpperInvariant();
            if (upper.IndexOfAny(UpperSpecialChars) < 0) return upper;
            var sb = new StringBuilder(upper.Length + 4);
            foreach (char c in upper)
            {
                string? expanded = c switch
                {
                    '\u00DF' => "SS",
                    '\u0149' => "\u02BCN",
                    '\u01F0' => "J\u030C",
                    '\u0587' => "\u0535\u0552",
                    '\uFB00' => "FF",
                    '\uFB01' => "FI",
                    '\uFB02' => "FL",
                    '\uFB03' => "FFI",
                    '\uFB04' => "FFL",
                    '\uFB05' => "ST",
                    '\uFB06' => "ST",
                    _ => null,
                };
                if (expanded != null) sb.Append(expanded);
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static readonly char[] UpperSpecialChars =
        {
            '\u00DF', '\u0149', '\u01F0', '\u0587', '\uFB00', '\uFB01', '\uFB02', '\uFB03', '\uFB04', '\uFB05', '\uFB06',
        };

        /// <summary>
        /// String.prototype.toLowerCase: invariant mapping plus İ → i̇ (the one
        /// unconditional expansion). Final-sigma context rules are not reproduced.
        /// </summary>
        internal static string ToLower(string s)
        {
            string lower = s.ToLowerInvariant();
            if (s.IndexOf('\u0130') < 0) return lower;
            var sb = new StringBuilder(s.Length + 2);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\u0130') sb.Append("i\u0307");
                else sb.Append(lower[i]);
            }
            return sb.ToString();
        }

        /// <summary>The start/end resolution String/Array.prototype.slice uses.</summary>
        internal static int RelativeIndex(double relative, int length)
        {
            double r = JsNumber.ToIntegerOrInfinity(relative);
            if (double.IsNegativeInfinity(r)) return 0;
            if (r < 0) return (int)Math.Max(length + r, 0);
            return (int)Math.Min(r, length);
        }

        /// <summary>String.prototype.slice(start, end); <paramref name="end"/> null = JS undefined.</summary>
        internal static string Slice(string s, double start, double? end)
        {
            int from = RelativeIndex(start, s.Length);
            int to = end.HasValue ? RelativeIndex(end.Value, s.Length) : s.Length;
            return to > from ? s.Substring(from, to - from) : string.Empty;
        }

        /// <summary>String.prototype.repeat, with the RangeError cases turned into exceptions.</summary>
        internal static string Repeat(string s, double count)
        {
            double n = JsNumber.ToIntegerOrInfinity(count);
            if (n < 0 || double.IsPositiveInfinity(n)) throw new ArgumentOutOfRangeException(nameof(count), "Invalid count value");
            if (n == 0 || s.Length == 0) return string.Empty;
            if (n * s.Length > MaxBuiltLength) throw new ArgumentOutOfRangeException(nameof(count), "Invalid string length");
            var sb = new StringBuilder(s.Length * (int)n);
            for (int i = 0; i < (int)n; i++) sb.Append(s);
            return sb.ToString();
        }

        /// <summary>String.prototype.padStart / padEnd.</summary>
        internal static string Pad(string s, double maxLength, string filler, bool atEnd)
        {
            double intMax = JsNumber.ToIntegerOrInfinity(maxLength);
            if (intMax <= s.Length || filler.Length == 0) return s;
            if (intMax > MaxBuiltLength) throw new ArgumentOutOfRangeException(nameof(maxLength), "Invalid string length");
            int fillLen = (int)intMax - s.Length;
            var sb = new StringBuilder((int)intMax);
            if (atEnd) sb.Append(s);
            while (fillLen > 0)
            {
                int take = Math.Min(fillLen, filler.Length);
                sb.Append(filler, 0, take);
                fillLen -= take;
            }
            if (!atEnd) sb.Append(s);
            return sb.ToString();
        }

        /// <summary>String.prototype.split with a string separator (never a regex).</summary>
        internal static string[] Split(string s, string separator)
        {
            if (separator.Length == 0)
            {
                // "".split("") is [] and "abc".split("") is one entry per UTF-16 unit.
                var units = new string[s.Length];
                for (int i = 0; i < s.Length; i++) units[i] = s[i].ToString();
                return units;
            }
            return s.Split(separator, StringSplitOptions.None);
        }

        /// <summary>The string iterator (for...of / spread): one entry per code point.</summary>
        internal static List<string> CodePoints(string s)
        {
            var list = new List<string>(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
                {
                    list.Add(s.Substring(i, 2));
                    i++;
                }
                else
                {
                    list.Add(s[i].ToString());
                }
            }
            return list;
        }

        /// <summary>
        /// ES GetSubstitution, as V8 implements it: <c>$$ $&amp; $` $' $n $nn $&lt;name&gt;</c>.
        /// <paramref name="captures"/> holds groups 1..m (null = group did not
        /// participate); <paramref name="named"/> is null when the pattern has no
        /// named groups, which makes <c>$&lt;</c> literal.
        /// </summary>
        internal static void AppendSubstitution(
            StringBuilder sb,
            string matched,
            string str,
            int position,
            IReadOnlyList<string?> captures,
            Func<string, string?>? named,
            string replacement)
        {
            int i = 0;
            int capturesLength = captures.Count + 1;
            while (i < replacement.Length)
            {
                char c = replacement[i];
                if (c != '$' || i + 1 >= replacement.Length)
                {
                    sb.Append(c);
                    i++;
                    continue;
                }
                char p = replacement[i + 1];
                switch (p)
                {
                    case '$':
                        sb.Append('$');
                        i += 2;
                        break;
                    case '&':
                        sb.Append(matched);
                        i += 2;
                        break;
                    case '`':
                        sb.Append(str, 0, position);
                        i += 2;
                        break;
                    case '\'':
                        {
                            int tail = position + matched.Length;
                            if (tail < str.Length) sb.Append(str, tail, str.Length - tail);
                            i += 2;
                            break;
                        }
                    case >= '0' and <= '9':
                        {
                            int index = p - '0';
                            int advance = 1;
                            if (i + 2 < replacement.Length && replacement[i + 2] >= '0' && replacement[i + 2] <= '9')
                            {
                                int twoDigit = index * 10 + (replacement[i + 2] - '0');
                                if (twoDigit < capturesLength)
                                {
                                    index = twoDigit;
                                    advance = 2;
                                }
                            }
                            if (index == 0 || index >= capturesLength)
                            {
                                // Not a capture reference: the '$' is literal and the
                                // digit is read again as ordinary text.
                                sb.Append('$');
                                i += 1;
                                break;
                            }
                            string? capture = captures[index - 1];
                            if (capture != null) sb.Append(capture);
                            i += 1 + advance;
                            break;
                        }
                    case '<':
                        {
                            int close = named == null ? -1 : replacement.IndexOf('>', i + 2);
                            if (close < 0)
                            {
                                sb.Append('$');
                                i += 1;
                                break;
                            }
                            string? value = named!(replacement.Substring(i + 2, close - (i + 2)));
                            if (value != null) sb.Append(value);
                            i = close + 1;
                            break;
                        }
                    default:
                        sb.Append('$');
                        i += 1;
                        break;
                }
            }
        }

        /// <summary>String.prototype.replace(string, string): first occurrence, $-patterns applied.</summary>
        internal static string ReplaceFirst(string subject, string find, string replacement)
        {
            int at = subject.IndexOf(find, StringComparison.Ordinal);
            if (at < 0) return subject;
            var sb = new StringBuilder(subject.Length + replacement.Length);
            sb.Append(subject, 0, at);
            AppendSubstitution(sb, find, subject, at, Array.Empty<string?>(), null, replacement);
            sb.Append(subject, at + find.Length, subject.Length - at - find.Length);
            return sb.ToString();
        }

        /// <summary>The JSON.stringify string quoting (QuoteJSONString), well-formed variant.</summary>
        internal static void AppendJsonQuoted(StringBuilder sb, string s)
        {
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            AppendUnicodeEscape(sb, c);
                        }
                        else if (char.IsHighSurrogate(c))
                        {
                            if (i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
                            {
                                sb.Append(c).Append(s[i + 1]);
                                i++;
                            }
                            else
                            {
                                AppendUnicodeEscape(sb, c);
                            }
                        }
                        else if (char.IsLowSurrogate(c))
                        {
                            AppendUnicodeEscape(sb, c);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
        }

        private static void AppendUnicodeEscape(StringBuilder sb, char c)
        {
            sb.Append("\\u").Append(((int)c).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>The escapeRegex helper in value-ops.ts: escapes <c>\ ^ $ . * + ? ( ) [ ] { } |</c>.</summary>
        internal static string EscapeRegex(string s)
        {
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                if (c is '\\' or '^' or '$' or '.' or '*' or '+' or '?' or '(' or ')' or '[' or ']' or '{' or '}' or '|')
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
