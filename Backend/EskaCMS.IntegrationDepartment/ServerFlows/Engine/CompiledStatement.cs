namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// A parsed condition statement (e.g. <c>C1 and (C2 or not C3)</c>). Immutable, so
    /// one instance can be cached and evaluated concurrently.
    /// </summary>
    public sealed class CompiledStatement
    {
        private readonly Node _root;

        internal CompiledStatement(Node root, int[] referencedIds)
        {
            _root = root;
            ReferencedIds = Array.AsReadOnly(referencedIds);
        }

        /// <summary>The distinct condition ids the statement mentions, in order of first appearance.</summary>
        public IReadOnlyCollection<int> ReferencedIds { get; }

        /// <summary>
        /// Evaluates the statement, short-circuiting <c>and</c>/<c>or</c> the way
        /// expr-eval does: the right side is only asked for when it can change the
        /// answer. <paramref name="conditionResult"/> may throw for an id with no
        /// result — the browser's "undefined variable" — and that exception then only
        /// happens where the browser would also throw (callers treat it as false, as
        /// <c>evaluateConditionStatement</c> does).
        /// </summary>
        public bool Evaluate(Func<int, bool> conditionResult)
        {
            if (conditionResult == null) throw new ArgumentNullException(nameof(conditionResult));
            return _root.Evaluate(conditionResult);
        }

        internal abstract class Node
        {
            internal abstract bool Evaluate(Func<int, bool> conditionResult);
        }

        internal sealed class RefNode : Node
        {
            private readonly int _id;

            internal RefNode(int id)
            {
                _id = id;
            }

            internal override bool Evaluate(Func<int, bool> conditionResult) => conditionResult(_id);
        }

        internal sealed class NotNode : Node
        {
            private readonly Node _operand;

            internal NotNode(Node operand)
            {
                _operand = operand;
            }

            internal override bool Evaluate(Func<int, bool> conditionResult) => !_operand.Evaluate(conditionResult);
        }

        /// <summary>An n-ary and/or, so long chains stay flat instead of deep.</summary>
        internal sealed class ChainNode : Node
        {
            private readonly bool _isAnd;
            private readonly Node[] _operands;

            internal ChainNode(bool isAnd, Node[] operands)
            {
                _isAnd = isAnd;
                _operands = operands;
            }

            internal override bool Evaluate(Func<int, bool> conditionResult)
            {
                foreach (Node operand in _operands)
                {
                    bool value = operand.Evaluate(conditionResult);
                    if (_isAnd && !value) return false;
                    if (!_isAnd && value) return true;
                }
                return _isAnd;
            }
        }
    }
}
