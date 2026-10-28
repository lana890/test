using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.IntegrationDepartment.ServerFlows.Engine;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Execution
{
    /// <summary>A mapping marked "required" resolved to nothing. The call is not sent.</summary>
    public sealed class FlowMappingException : Exception
    {
        public FlowMappingException(string message) : base(message) { }
    }

    /// <summary>
    /// Server port of the browser's prepareDataWithContext / resolveMappingCore
    /// (dynamic-api-caller.service.ts) for the typeOfValue values the parser allows. The order of
    /// the after-steps is the browser's: defaultValue when null/undefined, then valueOps, then the
    /// required check. "undefined" values are left out of the body, as JSON.stringify does.
    /// </summary>
    public sealed class FlowMappingResolver
    {
        private readonly FlowRunData _data;
        private JObject? _conditionData;

        public FlowMappingResolver(FlowRunData data)
        {
            _data = data;
        }

        /// <summary>
        /// The request body. <paramref name="localTree"/> follows the browser's local-endpoint path
        /// (buildLocalTreeMapping): mappings without a paramName are skipped and nothing is spread.
        /// </summary>
        public JToken? Prepare(JArray mappings, JToken? staticData, bool mergeData, bool localTree)
        {
            var data = new JObject();
            var resolvedConditional = new HashSet<string>(StringComparer.Ordinal);

            foreach (var token in mappings)
            {
                if (token is not JObject ele)
                    continue;

                var param = (string?)ele["paramName"];
                if (localTree && string.IsNullOrEmpty(param))
                    continue;

                var hasCondition = IsTrue(ele["hasCondition"]);
                var key = param ?? "undefined"; // data[undefined] in the browser

                if (hasCondition && resolvedConditional.Contains(key))
                    continue;

                if (hasCondition && ele["conditionalList"]?["conditions"] is JArray { Count: > 0 })
                {
                    _conditionData ??= _data.ConditionData();
                    if (!FlowConditions.Check(ele["conditionalList"] as JObject, _conditionData, _data.Ops))
                        continue;
                }

                var value = ResolveCore(ele);

                if (!localTree && (IsTrue(ele["spreadValue"]) || string.IsNullOrEmpty(param)) && value is JObject spread)
                {
                    foreach (var p in spread.Properties())
                        data[p.Name] = p.Value.DeepClone();
                    continue;
                }

                if (value == null)
                    data.Remove(key);
                else
                    data[key] = value;

                if (hasCondition)
                    resolvedConditional.Add(key);
            }

            if (staticData != null)
            {
                if (!mergeData || staticData is not JObject extra)
                    return staticData.DeepClone();
                foreach (var p in extra.Properties())
                    data[p.Name] = p.Value.DeepClone();
                return data;
            }

            // Root-value convention: a single mapping with an empty paramName is the whole body.
            if (!localTree && data.Count == 1 && data.TryGetValue("", out var root))
                return root;

            return data;
        }

        /// <summary>One mapping's value, or null for "undefined".</summary>
        public JToken? ResolveCore(JObject ele)
        {
            var type = (string?)ele["typeOfValue"];
            var path = (string?)ele["path"] ?? "";

            JToken? value = type switch
            {
                // the api caller's own normalizeStaticValue (not condition-utils'): "string" is String(v)
                "static" or "categories" => StaticValues.NormalizeForMapping(ele["valueToSend"], (string?)ele["staticType"]),
                "null" => JValue.CreateNull(),
                "currentDate" => FlowDateFormat.FormatCurrentDate(_data.Ops.Now, (string?)ele["formatDate"], (string?)ele["formatDateOther"], _data.Ops.TimeZone),
                "fromOrder" => Clone(JsonPath.Get(_data.Order, path)),
                "fromCart" => Clone(JsonPath.Get(_data.Cart, path)),
                "fromAdditionalDetailsCart" => Clone(JsonPath.Get(_data.AdditionalDetails, path)),
                "fromInput" => Clone(JsonPath.Get(_data.Input, path)),
                // the signed-in user, as the pages read it (and its older name, fromUser)
                "loggedInUser" or "fromUser" => Clone(JsonPath.Get(_data.UserView, path)),
                "currentUsername" => Clone(_data.UserView?["username"]),
                // the pages send null for an option the user does not have
                "userOption" => Clone(FlowUser.Option(_data.User, path)) ?? JValue.CreateNull(),
                "previousResults" => Clone(JsonPath.Get(_data.Results, path)),
                "arrayOfValues" => ArrayOfValues(ele["inputs"] as JArray),
                "objectOfObject" => Prepare(ele["inputs"] as JArray ?? new JArray(), null, false, false) ?? new JObject(),
                // the request editor's row for an input nobody mapped: left out
                "undefined" => null,
                // inside a loop (FlowRunData.EnterItem)
                "currentItem" => Clone(_data.Results["currentItem"]),
                "currentItemKey" => Clone(JsonPath.Get(_data.Results["currentItem"], (string?)ele["key"] ?? "")),
                "currentIndex" => Clone(_data.Results["currentIndex"]),
                // The parser refuses every other type; reading the results matches the browser's default.
                _ => path.Length > 0 ? Clone(JsonPath.Get(_data.Results, path)) : null,
            };

            if (Js.IsNil(value) && ele.TryGetValue("defaultValue", StringComparison.Ordinal, out var fallback))
                value = fallback?.DeepClone();

            if (ele["valueOps"] is JArray { Count: > 0 } ops)
                value = ValueOps.Apply(value, ops, _data.Ops);

            // The page's transform, after the ops (as the browser). Unlike the browser, a script
            // that throws stops here: sending the value untransformed to an insurer is worse.
            if (ele["transform"] is { Type: JTokenType.String } transform && ((string)transform!).Trim().Length > 0 && value != null)
            {
                if (_data.Ops.RunScript == null)
                    throw new FlowMappingException($"The transform of \"{(string?)ele["paramName"]}\" cannot run here.");
                try
                {
                    value = _data.Ops.RunScript("value", (string)transform!, value);
                }
                catch (FlowScriptException ex)
                {
                    throw new FlowMappingException($"The transform of \"{(string?)ele["paramName"]}\" failed: {ex.Message}");
                }
            }

            // Stricter than the browser (which checks undefined only): on the server a required value
            // that came back as JSON null (e.g. an id the answer did not carry) must not be sent either.
            if (IsTrue(ele["required"]) && Js.IsNil(value))
                throw new FlowMappingException($"Required mapping field \"{(string?)ele["paramName"]}\" is missing.");

            return value;
        }

        private JArray ArrayOfValues(JArray? inputs)
        {
            var items = new JArray();
            foreach (var input in inputs ?? new JArray())
            {
                if (input is not JObject item)
                    continue;
                var value = ResolveCore(item);
                if (value != null)
                    items.Add(value);
            }
            return items;
        }

        private static JToken? Clone(JToken? token) => token?.DeepClone();

        private static bool IsTrue(JToken? token) => token?.Type == JTokenType.Boolean && (bool)token;
    }
}
