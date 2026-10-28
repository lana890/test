using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>condition-utils.util.ts <c>evaluateConditionOperator</c>.</summary>
    public static class ConditionOperators
    {
        private static readonly string[] AllNames =
        {
            "Equals", "Not Equal", "Strict Equals", "Strict Not Equall",
            "Larger Than", "Larger Than Equal", "Less Than", "Less Than Equal",
            "And", "Or", "Not", "notFalsy", "Not Not",
            "Contains", "includes", "StartsWith", "EndsWith",
            "IsNull", "IsNotNull", "IsEmpty", "IsNotEmpty", "TypeOf",
        };

        /// <summary>
        /// Every operator the runtime knows, spelled exactly as configs store them —
        /// including the runtime's typo "Strict Not Equall".
        /// </summary>
        public static IReadOnlyCollection<string> Names { get; } = Array.AsReadOnly(AllNames);

        /// <summary>
        /// Evaluates <paramref name="op"/> with JS semantics: <c>==</c>/<c>===</c>, relational
        /// comparison (string vs string by code unit, otherwise numeric; NaN → false),
        /// truthiness, <c>String(x).includes</c> etc. An unknown operator is false.
        /// Dates coerced to text use UTC; see the zone overload.
        /// </summary>
        public static bool Evaluate(string? op, JToken? left, JToken? right) => Evaluate(op, left, right, TimeZoneInfo.Utc);

        /// <summary>As <see cref="Evaluate(string?, JToken?, JToken?)"/>, with dates coerced to text in <paramref name="zone"/>.</summary>
        public static bool Evaluate(string? op, JToken? left, JToken? right, TimeZoneInfo zone)
        {
            switch (op)
            {
                case "Equals":
                    return Js.LooseEquals(left, right, zone);
                case "Not Equal":
                    return !Js.LooseEquals(left, right, zone);
                case "Strict Equals":
                    return Js.StrictEquals(left, right);
                case "Strict Not Equall":
                    return !Js.StrictEquals(left, right);
                case "Larger Than":
                    return Js.GreaterThan(left, right);
                case "Larger Than Equal":
                    return Js.GreaterOrEqual(left, right);
                case "Less Than":
                    return Js.IsLess(left, right);
                case "Less Than Equal":
                    return Js.LessOrEqual(left, right);
                case "And":
                    return Js.Truthy(left) && Js.Truthy(right);
                case "Or":
                    return Js.Truthy(left) || Js.Truthy(right);
                case "Not":
                case "notFalsy":
                    return !Js.Truthy(left);
                case "Not Not":
                    return Js.Truthy(left);
                case "Contains":
                case "includes":
                    return Js.ToJsString(left, zone).Contains(Js.ToJsString(right, zone), StringComparison.Ordinal);
                case "StartsWith":
                    return Js.ToJsString(left, zone).StartsWith(Js.ToJsString(right, zone), StringComparison.Ordinal);
                case "EndsWith":
                    return Js.ToJsString(left, zone).EndsWith(Js.ToJsString(right, zone), StringComparison.Ordinal);
                case "IsNull":
                    return Js.IsNil(left) || IsEmptyString(left);
                case "IsNotNull":
                    return !Js.IsNil(left) && !IsEmptyString(left);
                case "IsEmpty":
                    return IsEmptyOperand(left);
                case "IsNotEmpty":
                    return !IsEmptyOperand(left);
                case "TypeOf":
                    return RuntimeType(left) == JsStrings.ToLower(Js.ToJsString(right, zone));
                default:
                    return false;
            }
        }

        private static bool IsEmptyString(JToken? v) => Js.TypeOf(v) == JsType.String && Js.StringValue(v!).Length == 0;

        /// <summary>
        /// The IsEmpty operator — not the same as value-ops isEmpty: NaN is empty, and a
        /// Date is empty too (it is an object with no own keys).
        /// </summary>
        private static bool IsEmptyOperand(JToken? left)
        {
            switch (Js.TypeOf(left))
            {
                case JsType.Undefined:
                case JsType.Null:
                    return true;
                case JsType.String:
                    return JsStrings.Trim(Js.StringValue(left!)).Length == 0;
                case JsType.Array:
                    return ((JArray)left!).Count == 0;
                case JsType.Date:
                    return true;
                case JsType.Object:
                    return !(left is JObject o) || o.Count == 0;
                case JsType.Number:
                    return double.IsNaN(Js.NumberValue(left!));
                default:
                    return false;
            }
        }

        /// <summary>getRuntimeType: <c>typeof</c>, with "null" and "array" split out.</summary>
        private static string RuntimeType(JToken? v)
        {
            switch (Js.TypeOf(v))
            {
                case JsType.Undefined: return "undefined";
                case JsType.Null: return "null";
                case JsType.Boolean: return "boolean";
                case JsType.Number: return "number";
                case JsType.String: return "string";
                case JsType.Array: return "array";
                default: return "object";
            }
        }
    }
}
