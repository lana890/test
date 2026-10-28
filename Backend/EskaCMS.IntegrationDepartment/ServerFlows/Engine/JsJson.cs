using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// <c>JSON.parse</c> and <c>JSON.stringify</c> with the browser's exact rules.
    /// Newtonsoft is deliberately not used for parsing: it is lenient (comments,
    /// single quotes, unquoted keys, NaN) and turns ISO strings into dates, so a
    /// string the browser keeps as text would come back as something else.
    /// </summary>
    internal static class JsJson
    {
        /// <summary>
        /// Nesting deeper than this is refused. JS would hit its own stack limit at
        /// several thousand levels; a server port must not risk a StackOverflow,
        /// which cannot be caught.
        /// </summary>
        private const int MaxDepth = 512;

        internal static JToken Parse(string text)
        {
            var parser = new Parser(text);
            parser.SkipWhiteSpace();
            JToken value = parser.ParseValue(0);
            parser.SkipWhiteSpace();
            if (!parser.AtEnd) throw parser.Error("Unexpected non-whitespace character after JSON");
            return value;
        }

        internal static bool TryParse(string text, out JToken? value)
        {
            try
            {
                value = Parse(text);
                return true;
            }
            catch (FormatException)
            {
                value = null;
                return false;
            }
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;

            internal Parser(string s)
            {
                _s = s;
            }

            internal bool AtEnd => _i >= _s.Length;

            internal FormatException Error(string message) => new FormatException(message + " at position " + _i.ToString(CultureInfo.InvariantCulture));

            internal void SkipWhiteSpace()
            {
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r') _i++;
                    else break;
                }
            }

            internal JToken ParseValue(int depth)
            {
                if (depth > MaxDepth) throw Error("JSON nested too deeply");
                if (AtEnd) throw Error("Unexpected end of JSON input");
                char c = _s[_i];
                switch (c)
                {
                    case '{':
                        return ParseObject(depth);
                    case '[':
                        return ParseArray(depth);
                    case '"':
                        return new JValue(ParseString());
                    case 't':
                        ExpectWord("true");
                        return new JValue(true);
                    case 'f':
                        ExpectWord("false");
                        return new JValue(false);
                    case 'n':
                        ExpectWord("null");
                        return JValue.CreateNull();
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber();
                        throw Error("Unexpected token " + c);
                }
            }

            private void ExpectWord(string word)
            {
                if (_i + word.Length > _s.Length || string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0) throw Error("Unexpected token");
                _i += word.Length;
            }

            private JObject ParseObject(int depth)
            {
                var obj = new JObject();
                _i++; // {
                SkipWhiteSpace();
                if (!AtEnd && _s[_i] == '}')
                {
                    _i++;
                    return obj;
                }
                while (true)
                {
                    SkipWhiteSpace();
                    if (AtEnd || _s[_i] != '"') throw Error("Expected property name");
                    string key = ParseString();
                    SkipWhiteSpace();
                    if (AtEnd || _s[_i] != ':') throw Error("Expected ':'");
                    _i++;
                    SkipWhiteSpace();
                    JToken value = ParseValue(depth + 1);
                    // Duplicate key: last value wins, first position kept — as in JS.
                    JsObjects.SetOwnRaw(obj, key, value);
                    SkipWhiteSpace();
                    if (AtEnd) throw Error("Unterminated object");
                    if (_s[_i] == ',')
                    {
                        _i++;
                        continue;
                    }
                    if (_s[_i] == '}')
                    {
                        _i++;
                        return obj;
                    }
                    throw Error("Expected ',' or '}'");
                }
            }

            private JArray ParseArray(int depth)
            {
                var arr = new JArray();
                _i++; // [
                SkipWhiteSpace();
                if (!AtEnd && _s[_i] == ']')
                {
                    _i++;
                    return arr;
                }
                while (true)
                {
                    SkipWhiteSpace();
                    arr.Add(ParseValue(depth + 1));
                    SkipWhiteSpace();
                    if (AtEnd) throw Error("Unterminated array");
                    if (_s[_i] == ',')
                    {
                        _i++;
                        continue;
                    }
                    if (_s[_i] == ']')
                    {
                        _i++;
                        return arr;
                    }
                    throw Error("Expected ',' or ']'");
                }
            }

            private string ParseString()
            {
                _i++; // opening quote
                StringBuilder? sb = null;
                int runStart = _i;
                while (true)
                {
                    if (AtEnd) throw Error("Unterminated string");
                    char c = _s[_i];
                    if (c == '"')
                    {
                        string tail = _s.Substring(runStart, _i - runStart);
                        _i++;
                        return sb == null ? tail : sb.Append(tail).ToString();
                    }
                    if (c < 0x20) throw Error("Bad control character in string");
                    if (c != '\\')
                    {
                        _i++;
                        continue;
                    }
                    sb ??= new StringBuilder();
                    sb.Append(_s, runStart, _i - runStart);
                    _i++;
                    if (AtEnd) throw Error("Unterminated string");
                    char e = _s[_i];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            {
                                int code = 0;
                                for (int k = 1; k <= 4; k++)
                                {
                                    if (_i + k >= _s.Length) throw Error("Bad Unicode escape");
                                    int h = HexValue(_s[_i + k]);
                                    if (h < 0) throw Error("Bad Unicode escape");
                                    code = code * 16 + h;
                                }
                                sb.Append((char)code);
                                _i += 4;
                                break;
                            }
                        default:
                            throw Error("Bad escaped character");
                    }
                    _i++;
                    runStart = _i;
                }
            }

            private static int HexValue(char c)
            {
                if (c >= '0' && c <= '9') return c - '0';
                if (c >= 'a' && c <= 'f') return c - 'a' + 10;
                if (c >= 'A' && c <= 'F') return c - 'A' + 10;
                return -1;
            }

            private JToken ParseNumber()
            {
                int start = _i;
                if (_s[_i] == '-') _i++;
                if (AtEnd) throw Error("No number after minus sign");
                if (_s[_i] == '0')
                {
                    _i++;
                }
                else if (_s[_i] >= '1' && _s[_i] <= '9')
                {
                    while (!AtEnd && JsNumber.IsAsciiDigit(_s[_i])) _i++;
                }
                else
                {
                    throw Error("No number after minus sign");
                }
                if (!AtEnd && _s[_i] == '.')
                {
                    _i++;
                    if (AtEnd || !JsNumber.IsAsciiDigit(_s[_i])) throw Error("Unterminated fractional number");
                    while (!AtEnd && JsNumber.IsAsciiDigit(_s[_i])) _i++;
                }
                if (!AtEnd && (_s[_i] == 'e' || _s[_i] == 'E'))
                {
                    _i++;
                    if (!AtEnd && (_s[_i] == '+' || _s[_i] == '-')) _i++;
                    if (AtEnd || !JsNumber.IsAsciiDigit(_s[_i])) throw Error("Exponent part is missing a number");
                    while (!AtEnd && JsNumber.IsAsciiDigit(_s[_i])) _i++;
                }
                double d = double.Parse(_s.AsSpan(start, _i - start), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
                return Js.FromNumber(d)!;
            }
        }

        /// <summary>JSON.stringify(v, null, indent); null for undefined.</summary>
        internal static string? Stringify(JToken? v, string? indent)
        {
            if (!IsSerializable(v)) return null;
            var sb = new StringBuilder();
            Write(sb, v!, indent ?? string.Empty, string.Empty, 0);
            return sb.ToString();
        }

        /// <summary>Values JSON.stringify skips: undefined (functions/symbols cannot occur here).</summary>
        private static bool IsSerializable(JToken? v) => Js.TypeOf(v) != JsType.Undefined;

        private static void Write(StringBuilder sb, JToken v, string gap, string indent, int depth)
        {
            if (depth > MaxDepth) throw new InvalidOperationException("Value nested too deeply to stringify");
            switch (Js.TypeOf(v))
            {
                case JsType.Null:
                    sb.Append("null");
                    return;
                case JsType.Boolean:
                    sb.Append(Js.Truthy(v) ? "true" : "false");
                    return;
                case JsType.Number:
                    {
                        double d = Js.NumberValue(v);
                        sb.Append(double.IsNaN(d) || double.IsInfinity(d) ? "null" : JsNumber.ToJsString(d));
                        return;
                    }
                case JsType.String:
                    JsStrings.AppendJsonQuoted(sb, Js.StringValue(v));
                    return;
                case JsType.Date:
                    JsStrings.AppendJsonQuoted(sb, JsDate.ToIsoString(Js.DateMs(v)));
                    return;
                case JsType.Array:
                    {
                        var arr = (JArray)v;
                        if (arr.Count == 0)
                        {
                            sb.Append("[]");
                            return;
                        }
                        string stepIndent = indent + gap;
                        sb.Append('[');
                        for (int i = 0; i < arr.Count; i++)
                        {
                            if (i > 0) sb.Append(',');
                            if (gap.Length > 0) sb.Append('\n').Append(stepIndent);
                            JToken? item = JsObjects.Norm(arr[i]);
                            if (IsSerializable(item)) Write(sb, item!, gap, stepIndent, depth + 1);
                            else sb.Append("null");
                        }
                        if (gap.Length > 0) sb.Append('\n').Append(indent);
                        sb.Append(']');
                        return;
                    }
                default:
                    {
                        if (!(v is JObject obj))
                        {
                            sb.Append("{}");
                            return;
                        }
                        string stepIndent = indent + gap;
                        bool any = false;
                        foreach (JProperty p in JsObjects.OwnProperties(obj))
                        {
                            JToken? value = JsObjects.Norm(p.Value);
                            if (!IsSerializable(value)) continue;
                            sb.Append(any ? "," : "{");
                            if (gap.Length > 0) sb.Append('\n').Append(stepIndent);
                            JsStrings.AppendJsonQuoted(sb, p.Name);
                            sb.Append(gap.Length > 0 ? ": " : ":");
                            Write(sb, value!, gap, stepIndent, depth + 1);
                            any = true;
                        }
                        if (!any)
                        {
                            sb.Append("{}");
                            return;
                        }
                        if (gap.Length > 0) sb.Append('\n').Append(indent);
                        sb.Append('}');
                        return;
                    }
            }
        }
    }
}
