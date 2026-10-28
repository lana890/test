using System.Collections.Concurrent;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.IntegrationDepartment.ServerFlows.Engine;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Execution
{
    /// <summary>
    /// Server port of the browser's checkConditionsFun + resolveConditionData (condition-utils.util.ts),
    /// limited to the getDataFrom values the parser allows. Same results as the browser, including
    /// the odd ones the parser warns about: no conditions or a blank statement is TRUE, a missing
    /// statement is FALSE.
    /// </summary>
    public static class FlowConditions
    {
        private static readonly ConcurrentDictionary<string, CompiledStatement?> Statements = new(StringComparer.Ordinal);

        /// <summary>Evaluates { conditions, statement } against <paramref name="data"/>.</summary>
        public static bool Check(JObject? list, JToken? data, ValueOpsContext ops)
        {
            if (list == null)
                return true;

            if (list["conditions"] is not JArray conditions || conditions.Count == 0)
                return true;

            var statementToken = list["statement"];
            var statement = statementToken?.Type == JTokenType.String ? (string)statementToken! : null;
            if (statement != null && statement.Trim().Length == 0)
                return true;
            if (statement == null)
                return false; // the browser evaluates "" and the parser throws: false

            var compiled = Statements.GetOrAdd(statement, s => ConditionStatement.TryCompile(s, out var c, out _) ? c : null);
            if (compiled == null)
                return false;

            // null = undefined (an array function with no result): reading it fails the statement.
            var results = new Dictionary<int, bool?>();
            foreach (var token in conditions)
            {
                if (token is not JObject condition || !TryId(condition["id"], out var id))
                    continue;

                var left = ResolveSide(condition["leftCon"], data, ops);
                var right = ResolveSide(condition["rightCon"], data, ops);
                var op = (string?)condition["operator"];
                var getDataObj = condition["leftCon"]?["getDataObj"];

                bool? result;
                if (getDataObj?["useLeftSearchArray"]?.Type == JTokenType.Boolean && (bool)getDataObj["useLeftSearchArray"]!)
                {
                    var arrayKey = (string?)getDataObj["leftSearchArrayKey"] ?? "";
                    // left?.some(...): no array (or not an array) gives no result, which the
                    // statement reads as false.
                    result = left is JArray items
                        ? (string?)getDataObj["arrayFunction"] switch
                        {
                            "some" => items.Any(i => ConditionOperators.Evaluate(op, Property(i, arrayKey), right)),
                            "every" => items.All(i => ConditionOperators.Evaluate(op, Property(i, arrayKey), right)),
                            "notInArray" => !items.Any(i => ConditionOperators.Evaluate(op, Property(i, arrayKey), right)),
                            _ => null,
                        }
                        : null;
                }
                else
                {
                    result = ConditionOperators.Evaluate(op, left, right);
                }

                results[id] = result;
            }

            // The browser's parser (expr-eval-fork) short-circuits "and" / "or", and throws when it
            // reads a C<id> that has no condition or whose result is undefined; the statement is then
            // false. So "C1 or C3" is true when C1 is, and "C3 or C1" is false (checked 2026-10-11).
            try
            {
                return compiled.Evaluate(i => results.TryGetValue(i, out var r) && r.HasValue ? r.Value : throw new UnknownConditionException());
            }
            catch (UnknownConditionException)
            {
                return false;
            }
        }

        private sealed class UnknownConditionException : Exception { }

        /// <summary>One side of a condition ({ getDataObj: { getDataFrom, key, value, ... } }).</summary>
        public static JToken? ResolveSide(JToken? side, JToken? data, ValueOpsContext ops)
        {
            var getDataObj = side?["getDataObj"] as JObject;
            if (getDataObj == null)
                return null;

            var from = (string?)getDataObj["getDataFrom"];
            if (string.IsNullOrEmpty(from))
                return null;

            var key = (string?)getDataObj["key"] ?? "";
            var value = getDataObj["value"];

            JToken? result = from switch
            {
                "number" => value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.String && (string?)value == "")
                    ? new JValue(0)
                    : Js.FromNumber(Js.ToNumber(value)),
                "static" => StaticValues.Normalize(value, (string?)getDataObj["staticType"]),
                "true" => new JValue(true),
                "false" => new JValue(false),
                "null" => JValue.CreateNull(),
                "undefined" => null,
                "fromCart" => JsonPath.Get(data?["cartData"], key),
                "fromAdditionalDetailsCart" => JsonPath.Get(FirstDefined(data?["additionalDetailsCart"], data?["InputAdditionalDetails"], data?["additionalDetails"]), key),
                "fromOrder" => JsonPath.Get(data?["order"], key),
                "fromInput" => JsonPath.Get(data?["input"], key),
                "userInfo" or "fromUser" => JsonPath.Get(FlowUser.View(data?["user"]), key),
                "userOptions" => FlowUser.Option(data?["user"], key),
                "arrayData" => new JArray((getDataObj["arrayItems"] as JArray ?? new JArray())
                    .Select(item => ResolveSide(item, data, ops) ?? JValue.CreateNull())),
                // apiRes / previousApi and anything else: a path into the data itself.
                _ => JsonPath.Get(data, key),
            };

            if (getDataObj["valueOps"] is JArray { Count: > 0 } valueOps)
                result = ValueOps.Apply(result, valueOps, ops);

            // condition-utils: useTransformResponse + transformResponseScript ("value"), after the ops.
            // A script that throws stops the run (the browser would compare the untransformed value).
            if (getDataObj["useTransformResponse"]?.Type == JTokenType.Boolean && (bool)getDataObj["useTransformResponse"]!
                && getDataObj["transformResponseScript"] is { Type: JTokenType.String } script && ((string)script!).Trim().Length > 0
                && result != null && ops.RunScript != null)
                result = ops.RunScript("value", (string)script!, result);

            return result;
        }

        /// <summary>item[key] in the browser: a plain property, not a path.</summary>
        private static JToken? Property(JToken item, string key)
        {
            return item is JObject o && o.TryGetValue(key, StringComparison.Ordinal, out var v) ? v : null;
        }

        /// <summary>a ?? b ?? c, where only undefined and null fall through.</summary>
        private static JToken? FirstDefined(params JToken?[] tokens)
        {
            return tokens.FirstOrDefault(t => t != null && t.Type != JTokenType.Null);
        }

        private static bool TryId(JToken? token, out int id)
        {
            id = 0;
            return token?.Type switch
            {
                JTokenType.Integer => (id = (int)(long)token) > 0,
                JTokenType.String => int.TryParse((string?)token, out id) && id > 0,
                _ => false,
            };
        }
    }
}
