using System.Globalization;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// The server grammar for a conditionalList <c>statement</c>.
    /// </summary>
    /// <remarks>
    /// The browser hands the statement to expr-eval, where <c>||</c> is string
    /// concatenation (so a gate written with it is always true), <c>&amp;&amp;</c> throws,
    /// and <c>AND</c>/<c>c1</c> are unknown variables (the statement is then false).
    /// The server therefore accepts only what means the same thing in both places:
    /// <c>C&lt;n&gt;</c> (n a positive integer without leading zeros), parentheses, and
    /// lowercase <c>and</c>, <c>or</c>, <c>not</c>, with any whitespace between tokens.
    /// Precedence: not &gt; and &gt; or. Anything else is a compile error.
    /// An empty statement is an error here too; note that the browser treats a
    /// blank (whitespace-only) statement as "pass" and a missing one as "fail"
    /// (<c>checkConditionsFun</c>), which callers decide before compiling.
    /// </remarks>
    public static class ConditionStatement
    {
        /// <summary>Parenthesis/not nesting deeper than this is rejected, so evaluation can never overflow the stack.</summary>
        private const int MaxNesting = 100;

        /// <summary>Parses <paramref name="statement"/>; on failure <paramref name="error"/> says what and where.</summary>
        public static bool TryCompile(string? statement, out CompiledStatement? compiled, out string? error)
        {
            compiled = null;
            error = null;
            if (statement == null || statement.Trim().Length == 0)
            {
                error = "The statement is empty.";
                return false;
            }
            try
            {
                var parser = new Parser(statement);
                CompiledStatement.Node root = parser.ParseOr(0);
                if (!parser.AtEnd) throw parser.Fail("unexpected '" + parser.Current + "'");
                compiled = new CompiledStatement(root, parser.Ids.ToArray());
                return true;
            }
            catch (FormatException e)
            {
                error = e.Message;
                return false;
            }
        }

        private sealed class Parser
        {
            private readonly List<(string Text, int Position)> _tokens = new List<(string, int)>();
            private int _index;

            internal Parser(string s)
            {
                int i = 0;
                while (i < s.Length)
                {
                    char c = s[i];
                    if (char.IsWhiteSpace(c))
                    {
                        i++;
                        continue;
                    }
                    if (c == '(' || c == ')')
                    {
                        _tokens.Add((c.ToString(), i));
                        i++;
                        continue;
                    }
                    if (char.IsAsciiLetterOrDigit(c) || c == '_')
                    {
                        // One identifier-like run, as expr-eval reads it: "C1and" is a single (unknown) name.
                        int start = i;
                        while (i < s.Length && (char.IsAsciiLetterOrDigit(s[i]) || s[i] == '_')) i++;
                        _tokens.Add((s.Substring(start, i - start), start));
                        continue;
                    }
                    int symbolStart = i;
                    while (i < s.Length && !char.IsWhiteSpace(s[i]) && !char.IsAsciiLetterOrDigit(s[i]) && s[i] != '_' && s[i] != '(' && s[i] != ')') i++;
                    string symbol = s.Substring(symbolStart, i - symbolStart);
                    string hint = symbol == "&&" || symbol == "||" || symbol == "!"
                        ? " (use lowercase 'and', 'or', 'not'; in the browser '||' concatenates strings and '&&' throws)"
                        : string.Empty;
                    throw new FormatException("Unexpected '" + symbol + "' at position " + symbolStart.ToString(CultureInfo.InvariantCulture) + hint + ".");
                }
            }

            internal List<int> Ids { get; } = new List<int>();

            internal bool AtEnd => _index >= _tokens.Count;

            internal string Current => AtEnd ? "end of statement" : _tokens[_index].Text;

            internal FormatException Fail(string what)
            {
                string where = AtEnd ? "at the end" : "at position " + _tokens[_index].Position.ToString(CultureInfo.InvariantCulture);
                return new FormatException("Invalid statement: " + what + " " + where + ".");
            }

            internal CompiledStatement.Node ParseOr(int depth)
            {
                var operands = new List<CompiledStatement.Node> { ParseAnd(depth) };
                while (!AtEnd && Current == "or")
                {
                    _index++;
                    operands.Add(ParseAnd(depth));
                }
                return operands.Count == 1 ? operands[0] : new CompiledStatement.ChainNode(false, operands.ToArray());
            }

            private CompiledStatement.Node ParseAnd(int depth)
            {
                var operands = new List<CompiledStatement.Node> { ParseUnary(depth) };
                while (!AtEnd && Current == "and")
                {
                    _index++;
                    operands.Add(ParseUnary(depth));
                }
                return operands.Count == 1 ? operands[0] : new CompiledStatement.ChainNode(true, operands.ToArray());
            }

            private CompiledStatement.Node ParseUnary(int depth)
            {
                if (depth > MaxNesting) throw Fail("nesting deeper than " + MaxNesting.ToString(CultureInfo.InvariantCulture) + " levels");
                if (AtEnd) throw Fail("expected a condition (C<n>), 'not' or '('");
                string token = Current;
                if (token == "not")
                {
                    _index++;
                    return new CompiledStatement.NotNode(ParseUnary(depth + 1));
                }
                if (token == "(")
                {
                    _index++;
                    CompiledStatement.Node inner = ParseOr(depth + 1);
                    if (AtEnd || Current != ")") throw Fail("expected ')'");
                    _index++;
                    return inner;
                }
                if (TryReadId(token, out int id))
                {
                    _index++;
                    if (!Ids.Contains(id)) Ids.Add(id);
                    return new CompiledStatement.RefNode(id);
                }
                if (token == ")") throw Fail("unexpected ')'");
                if (token == "and" || token == "or") throw Fail("'" + token + "' needs a condition before it");
                if (string.Equals(token, "and", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(token, "or", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(token, "not", StringComparison.OrdinalIgnoreCase))
                {
                    throw Fail("'" + token + "' must be lowercase");
                }
                throw Fail("'" + token + "' is not a condition reference (C1, C2, ...)");
            }

            private static bool TryReadId(string token, out int id)
            {
                id = 0;
                if (token.Length < 2 || token[0] != 'C' || token[1] < '1' || token[1] > '9') return false;
                for (int i = 1; i < token.Length; i++)
                {
                    if (token[i] < '0' || token[i] > '9') return false;
                }
                return int.TryParse(token.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out id);
            }
        }
    }
}
