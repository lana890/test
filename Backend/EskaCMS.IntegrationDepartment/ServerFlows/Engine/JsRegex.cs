using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// A regex written in JavaScript syntax (as config authors write them for the
    /// browser), compiled for .NET with a match timeout.
    /// </summary>
    /// <remarks>
    /// Translation keeps JS meaning where .NET differs: <c>$</c> is end-of-input only,
    /// <c>.</c> excludes all four JS line terminators, <c>\s</c> is the JS whitespace
    /// set, <c>\d</c>/<c>\w</c> are ASCII (ECMAScript mode), <c>[]</c>/<c>[^]</c> mean
    /// "nothing"/"anything", letter escapes JS treats as identity escapes (<c>\a \e \A \z \p</c> …)
    /// stay literal, and inline flags such as <c>(?i)</c> are a syntax error as in JS.
    /// Capture numbers follow JS (opening-parenthesis order) even though .NET numbers
    /// named groups after unnamed ones. Anything that fails to compile throws
    /// <see cref="ArgumentException"/> — callers treat that like the JS SyntaxError.
    /// </remarks>
    internal sealed class JsRegex
    {
        /// <summary>Patterns come from config, so every match is time-boxed.</summary>
        internal static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);

        private const int MaxCacheEntries = 512;
        private static readonly ConcurrentDictionary<string, JsRegex> Cache = new ConcurrentDictionary<string, JsRegex>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, Regex> RawCache = new ConcurrentDictionary<string, Regex>(StringComparer.Ordinal);

        private readonly Regex _regex;
        private readonly int[] _groupNumbers; // JS group (1-based) -> .NET group number
        private readonly bool _hasNamedGroups;

        private JsRegex(Regex regex, int[] groupNumbers, bool hasNamedGroups)
        {
            _regex = regex;
            _groupNumbers = groupNumbers;
            _hasNamedGroups = hasNamedGroups;
        }

        /// <summary><c>new RegExp(pattern)</c>; throws <see cref="ArgumentException"/> where JS throws SyntaxError.</summary>
        internal static JsRegex Get(string pattern)
        {
            if (Cache.TryGetValue(pattern, out JsRegex? cached)) return cached;
            JsRegex compiled = Compile(pattern);
            if (Cache.Count >= MaxCacheEntries) Cache.Clear();
            Cache[pattern] = compiled;
            return compiled;
        }

        /// <summary>
        /// A .NET pattern the engine assembled itself (escaped literals only), cached
        /// and time-boxed the same way because its literal parts still come from config.
        /// </summary>
        internal static Regex Raw(string netPattern)
        {
            if (RawCache.TryGetValue(netPattern, out Regex? cached)) return cached;
            var regex = new Regex(netPattern, RegexOptions.CultureInvariant, MatchTimeout);
            if (RawCache.Count >= MaxCacheEntries) RawCache.Clear();
            RawCache[netPattern] = regex;
            return regex;
        }

        /// <summary>RegExp.prototype.test.</summary>
        internal bool IsMatch(string input) => _regex.IsMatch(input);

        /// <summary>String.prototype.replace(regex, string) with JS <c>$</c>-patterns; <paramref name="global"/> = the g flag.</summary>
        internal string Replace(string input, string replacement, bool global)
        {
            Match m = _regex.Match(input);
            if (!m.Success) return input;
            var sb = new StringBuilder(input.Length + 16);
            int copiedTo = 0;
            while (m.Success)
            {
                sb.Append(input, copiedTo, m.Index - copiedTo);
                var captures = new string?[_groupNumbers.Length];
                for (int i = 0; i < _groupNumbers.Length; i++)
                {
                    Group g = m.Groups[_groupNumbers[i]];
                    captures[i] = g.Success ? g.Value : null;
                }
                Match current = m;
                Func<string, string?>? named = null;
                if (_hasNamedGroups)
                {
                    named = name =>
                    {
                        int number = _regex.GroupNumberFromName(name);
                        if (number < 0) return null;
                        Group g = current.Groups[number];
                        return g.Success ? g.Value : null;
                    };
                }
                JsStrings.AppendSubstitution(sb, m.Value, input, m.Index, captures, named, replacement);
                copiedTo = m.Index + m.Length;
                if (!global) break;
                m = m.NextMatch();
            }
            sb.Append(input, copiedTo, input.Length - copiedTo);
            return sb.ToString();
        }

        private static JsRegex Compile(string pattern)
        {
            var groups = new List<string?>(); // null = unnamed, in JS (opening-parenthesis) order
            string net = Translate(pattern, groups);
            Regex regex;
            try
            {
                regex = new Regex(net, RegexOptions.ECMAScript, MatchTimeout);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException("Invalid regular expression: /" + pattern + "/: " + e.Message, nameof(pattern), e);
            }

            var numbers = new int[groups.Count];
            int unnamed = 0;
            bool hasNamed = false;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i] == null)
                {
                    numbers[i] = ++unnamed;
                }
                else
                {
                    hasNamed = true;
                    numbers[i] = regex.GroupNumberFromName(groups[i]!);
                }
            }
            return new JsRegex(regex, numbers, hasNamed);
        }

        private static string Translate(string p, List<string?> groups)
        {
            var sb = new StringBuilder(p.Length + 16);
            bool inClass = false;
            int i = 0;
            while (i < p.Length)
            {
                char c = p[i];
                if (c == '\\')
                {
                    if (i + 1 >= p.Length) throw new ArgumentException("Invalid regular expression: \\ at end of pattern");
                    i = TranslateEscape(p, i, inClass, sb);
                    continue;
                }
                if (inClass)
                {
                    if (c == ']') inClass = false;
                    // A '[' inside a JS class is literal; in .NET "-[" would start class subtraction.
                    sb.Append(c == '[' ? "\\[" : c.ToString());
                    i++;
                    continue;
                }
                switch (c)
                {
                    case '[':
                        if (i + 1 < p.Length && p[i + 1] == ']')
                        {
                            sb.Append("(?!)");
                            i += 2;
                            continue;
                        }
                        if (i + 2 < p.Length && p[i + 1] == '^' && p[i + 2] == ']')
                        {
                            sb.Append(@"[\s\S]");
                            i += 3;
                            continue;
                        }
                        inClass = true;
                        sb.Append('[');
                        i++;
                        if (i < p.Length && p[i] == '^')
                        {
                            sb.Append('^');
                            i++;
                        }
                        continue;
                    case '(':
                        if (i + 1 < p.Length && p[i + 1] == '?')
                        {
                            if (i + 2 < p.Length && (p[i + 2] == ':' || p[i + 2] == '=' || p[i + 2] == '!'))
                            {
                                sb.Append(p, i, 3);
                                i += 3;
                                continue;
                            }
                            if (i + 2 < p.Length && p[i + 2] == '<')
                            {
                                if (i + 3 < p.Length && (p[i + 3] == '=' || p[i + 3] == '!'))
                                {
                                    sb.Append(p, i, 4);
                                    i += 4;
                                    continue;
                                }
                                int close = p.IndexOf('>', i + 3);
                                if (close < 0) throw new ArgumentException("Invalid regular expression: invalid capture group name");
                                string name = p.Substring(i + 3, close - (i + 3));
                                if (name.Length == 0 || char.IsDigit(name[0])) throw new ArgumentException("Invalid regular expression: invalid capture group name");
                                groups.Add(name);
                                sb.Append("(?<").Append(name).Append('>');
                                i = close + 1;
                                continue;
                            }
                            throw new ArgumentException("Invalid regular expression: invalid group");
                        }
                        groups.Add(null);
                        sb.Append('(');
                        i++;
                        continue;
                    case '$':
                        sb.Append(@"\z");
                        i++;
                        continue;
                    case '.':
                        sb.Append(@"[^\n\r\u2028\u2029]");
                        i++;
                        continue;
                    default:
                        sb.Append(c);
                        i++;
                        continue;
                }
            }
            if (inClass) throw new ArgumentException("Invalid regular expression: missing /");
            return sb.ToString();
        }

        /// <summary>Translates the escape at <paramref name="i"/>; returns the index after it.</summary>
        private static int TranslateEscape(string p, int i, bool inClass, StringBuilder sb)
        {
            char e = p[i + 1];
            switch (e)
            {
                case 's':
                    sb.Append(inClass ? JsStrings.WhiteSpaceClassBody : "[" + JsStrings.WhiteSpaceClassBody + "]");
                    return i + 2;
                case 'S':
                    // Outside a class this is exact; inside one the ECMAScript-mode \S is the closest .NET has.
                    sb.Append(inClass ? @"\S" : "[^" + JsStrings.WhiteSpaceClassBody + "]");
                    return i + 2;
                case 'd':
                case 'D':
                case 'w':
                case 'W':
                case 'b':
                case 'B':
                case 'f':
                case 'n':
                case 'r':
                case 't':
                case 'v':
                case 'k':
                    sb.Append('\\').Append(e);
                    return i + 2;
                case 'c':
                    if (i + 2 < p.Length && IsAsciiLetter(p[i + 2]))
                    {
                        sb.Append(p, i, 3);
                        return i + 3;
                    }
                    // Annex B: a "\c" not followed by a letter is a literal backslash and 'c'.
                    sb.Append(@"\\c");
                    return i + 2;
                case 'x':
                    if (i + 3 < p.Length && IsHex(p[i + 2]) && IsHex(p[i + 3]))
                    {
                        sb.Append(p, i, 4);
                        return i + 4;
                    }
                    sb.Append('x');
                    return i + 2;
                case 'u':
                    if (i + 5 < p.Length && IsHex(p[i + 2]) && IsHex(p[i + 3]) && IsHex(p[i + 4]) && IsHex(p[i + 5]))
                    {
                        sb.Append(p, i, 6);
                        return i + 6;
                    }
                    sb.Append('u');
                    return i + 2;
                case '0':
                    if (i + 2 >= p.Length || !char.IsAsciiDigit(p[i + 2]))
                    {
                        sb.Append(@"\x00");
                        return i + 2;
                    }
                    sb.Append('\\').Append(e);
                    return i + 2;
                default:
                    if (char.IsAsciiDigit(e))
                    {
                        sb.Append('\\').Append(e); // backreference (or Annex B octal, same in ECMAScript mode)
                        return i + 2;
                    }
                    if (IsAsciiLetter(e) || e == '_')
                    {
                        // JS identity escape: "\a" is just "a" (.NET would read bell, \A anchor, \p{…} …).
                        sb.Append(e);
                        return i + 2;
                    }
                    sb.Append('\\').Append(e);
                    return i + 2;
            }
        }

        private static bool IsAsciiLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        private static bool IsHex(char c) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    }
}
