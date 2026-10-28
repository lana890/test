using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.IntegrationDepartment.ServerFlows.Engine;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Execution
{
    /// <summary>
    /// What a run reads while it resolves mappings and conditions:
    ///  - Context: frozen when the run was created - { order, cart, additionalDetails } (see FlowContextBuilder);
    ///  - Results: the declared outputs of the requests that finished, { request: { output: value } },
    ///    also reachable as { group: { request: { ... } } } like the browser's session results.
    /// </summary>
    public sealed class FlowRunData
    {
        public FlowRunData(JObject context, JObject outputs, FlowDefinition definition, ValueOpsContext ops)
        {
            Context = context;
            Ops = ops;
            Results = new JObject();
            foreach (var request in definition.Requests)
            {
                if (outputs[request.Name] is JObject or JArray)
                    Store(request, outputs[request.Name]!);
            }
        }

        public JObject Context { get; }

        public JObject Results { get; }

        public ValueOpsContext Ops { get; }

        public JToken? Order => Context["order"];

        public JToken? Cart => Context["cart"];

        public JToken? AdditionalDetails => Context["additionalDetails"];

        /// <summary>What the page sent (flows a page starts), only the inputs the flow accepts.</summary>
        public JToken? Input => Context["input"];

        /// <summary>The signed-in visitor (a page flow) or the order's customer, as stored on the run.</summary>
        public JToken? User => Context["user"];

        private JObject? _userView;

        /// <summary>The user as the pages read it (see <see cref="FlowUser"/>): username, fullName, email, mobilephone, userType, options.</summary>
        public JObject? UserView => _userView ??= FlowUser.View(User);

        /// <summary>Records a finished request's outputs under its name and under its group.</summary>
        public void SetOutputs(string group, string request, JToken outputs)
        {
            _conditionData = null;
            Results[request] = outputs.DeepClone();
            if (Results[group] is not JObject groupResults)
                Results[group] = groupResults = new JObject();
            groupResults[request] = outputs.DeepClone();
        }

        /// <summary>One output of a request that answers once; null when it has none (or answered a list).</summary>
        public JToken? OutputOf(FlowOutputRef reference) => (Results[reference.Request] as JObject)?[reference.Output];

        /// <summary>
        /// A finished request's result (its outputs, or the list of them for a loop) under every key
        /// the browser keeps it: its name, its group, a loop's resultKey, and for a conditional
        /// group's list the group itself.
        /// </summary>
        public void Store(FlowRequest request, JToken result)
        {
            _conditionData = null;
            SetOutputs(request.GroupName, request.Name, result);
            if (request.Loop?.ResultKey is { } key)
                Results[key] = result.DeepClone();
            if (request.Loop?.IsGroupSource == true && result is JArray)
                Results[request.GroupName] = result.DeepClone();
        }

        /// <summary>
        /// transformApiResponse: valueOps, the script ("response"), pick, omit, then readResFrom
        /// (when it finds something). A script that throws stops here (the browser keeps going
        /// with the unshaped answer).
        /// </summary>
        public JToken? Shape(FlowRequest request, JToken? response)
        {
            var shaped = response;
            if (request.TransformValueOps != null)
                shaped = ValueOps.Apply(shaped, request.TransformValueOps, Ops);
            if (request.TransformScript != null)
            {
                if (Ops.RunScript == null)
                    throw new FlowScriptException("Scripts cannot run here.");
                shaped = Ops.RunScript("response", request.TransformScript, shaped);
            }
            if (request.Pick.Count > 0)
            {
                var picked = new JObject();
                foreach (var field in request.Pick)
                {
                    var value = JsonPath.Get(shaped, field);
                    if (value != null)
                        picked[field] = value.DeepClone();
                }
                shaped = picked;
            }
            if (request.Omit.Count > 0)
            {
                // { ...t } then delete: only an object keeps its keys
                var copy = shaped is JObject o ? (JObject)o.DeepClone() : new JObject();
                foreach (var field in request.Omit)
                    copy.Remove(field);
                shaped = copy;
            }
            if (request.ReadResFrom != null)
            {
                var extracted = JsonPath.Get(shaped, request.ReadResFrom);
                if (extracted != null)
                    shaped = extracted;
            }
            return shaped;
        }

        /// <summary>The declared outputs read from a (shaped) answer.</summary>
        public JObject ReadOutputs(IReadOnlyDictionary<string, FlowOutput> declared, JToken? source)
        {
            var outputs = new JObject();
            foreach (var output in declared.Values)
            {
                var value = JsonPath.Get(source, output.Path)?.DeepClone();
                if (output.ValueOps != null)
                    value = ValueOps.Apply(value, output.ValueOps, Ops);
                value = JsonSafe(value);
                if (value != null)
                    outputs[output.Name] = value;
            }
            return outputs;
        }

        /// <summary>The group's transformResult over its result: valueOps, script ("result"), mapping.</summary>
        public void ApplyGroupTransform(FlowGroup group)
        {
            var transform = group.Transform;
            if (transform == null)
                return;
            JToken? result = Results[group.Name]?.DeepClone();
            if (transform.ValueOps != null)
                result = ValueOps.Apply(result, transform.ValueOps, Ops);
            if (transform.Script != null)
            {
                if (Ops.RunScript == null)
                    throw new FlowScriptException("Scripts cannot run here.");
                result = Ops.RunScript("result", transform.Script, result);
            }
            if (transform.Mapping != null)
            {
                var mapped = new JObject();
                foreach (var (key, path) in transform.Mapping)
                {
                    var value = JsonPath.Get(result, path);
                    if (value != null)
                        mapped[key] = value.DeepClone();
                }
                result = mapped;
            }
            _conditionData = null;
            Results[group.Name] = JsonSafe(result) ?? JValue.CreateNull();
        }

        /// <summary>
        /// What a call that failed for good answers when the run goes on without it: its fallback
        /// (fallbackValueMapping / fallbackValue) read through its outputs, else the browser's marker,
        /// { error, name } (a loop item's: { error, item, index }), with its outputs read from it as well.
        /// </summary>
        public JObject GivenUpOutputs(FlowRequest request, int? index, ref string reason)
        {
            JToken? instead = null;
            try
            {
                if (request.Fallback?.Mapping is { } mapping)
                    instead = new FlowMappingResolver(this).Prepare(mapping, null, false, false);
                else if (request.Fallback?.Value is { } value)
                    instead = value.DeepClone();
            }
            catch (Exception ex)
            {
                reason += $" (its fallback could not be built: {ex.Message})";
            }
            if (instead != null)
            {
                try
                {
                    return ReadOutputs(request.Outputs, instead);
                }
                catch (Exception ex)
                {
                    reason += $" (its fallback could not be read: {ex.Message})";
                }
            }

            var marker = index == null
                ? new JObject { ["error"] = reason, ["name"] = request.Name }
                : new JObject { ["error"] = reason, ["item"] = Results["currentItem"]?.DeepClone(), ["index"] = index };
            JObject outputs;
            try
            {
                outputs = ReadOutputs(request.Outputs, marker);
            }
            catch (Exception)
            {
                outputs = new JObject();
            }
            // what the pages read after "Keep going on error" stays readable as on the pages
            foreach (var p in marker.Properties().Where(p => outputs[p.Name] == null))
                outputs[p.Name] = p.Value.DeepClone();
            return outputs;
        }

        /// <summary>
        /// The loop's current item, readable while that item's call is built: previousResults
        /// currentItem / currentIndex / the alias (as the browser puts them in previousResults),
        /// and the currentItem, currentItemKey and currentIndex mappings.
        /// </summary>
        public void EnterItem(FlowLoop loop, JToken item, int index)
        {
            _conditionData = null;
            Results["currentItem"] = item.DeepClone();
            Results["currentIndex"] = index;
            if (loop.ItemAlias != null)
                Results[loop.ItemAlias] = item.DeepClone();
            _itemAlias = loop.ItemAlias;
        }

        public void LeaveItem()
        {
            _conditionData = null;
            Results.Remove("currentItem");
            Results.Remove("currentIndex");
            if (_itemAlias != null)
                Results.Remove(_itemAlias);
            _itemAlias = null;
        }

        private string? _itemAlias;

        private JObject? _conditionData;

        /// <summary>
        /// The object conditions read: the results first, then the named data, as the browser builds
        /// its executeIf context ({ ...base, ...sessionResults }). The parser reserves these names (and
        /// warns about a result named "user" or "input"). Built once and kept until the results change;
        /// conditions only read it.
        /// </summary>
        public JObject ConditionData()
        {
            if (_conditionData != null)
                return _conditionData;
            var data = new JObject();
            foreach (var p in Results.Properties())
                data[p.Name] = p.Value.DeepClone();
            data["cartData"] = Cart?.DeepClone();
            data["additionalDetails"] = AdditionalDetails?.DeepClone();
            data["order"] = Order?.DeepClone();
            data["input"] = Input?.DeepClone();
            data["user"] = User?.DeepClone();
            return _conditionData = data;
        }

        /// <summary>The outputs to store on the run: request entries only (group entries are rebuilt on load).</summary>
        public static JObject OutputsOnly(JObject results, IEnumerable<string> requestNames)
        {
            var names = new HashSet<string>(requestNames, StringComparer.Ordinal);
            var outputs = new JObject();
            foreach (var p in results.Properties().Where(p => names.Contains(p.Name)))
                outputs[p.Name] = p.Value.DeepClone();
            return outputs;
        }

        /// <summary>A copy of the date for a value op context in the flow's time zone.</summary>
        public static ValueOpsContext OpsAt(DateTimeOffset now, TimeZoneInfo zone) => new()
        {
            Now = now,
            TimeZone = zone,
            // the pages' JavaScript transforms, in the JsScript sandbox, dates in the flow's zone
            RunScript = (arg, script, value) => JsScript.Run(arg, script, value, zone),
        };

        /// <summary>
        /// The value as JSON.stringify would send it: undefined members dropped, undefined list items
        /// null, dates as ISO text. valueOps can leave JS undefined inside objects and lists, which
        /// Newtonsoft would write as a bare `undefined` (not JSON). Null for undefined itself.
        /// </summary>
        public static JToken? JsonSafe(JToken? value)
        {
            if (value == null || value.Type == JTokenType.Undefined)
                return null;
            var text = Js.Stringify(value);
            return text == null ? null : Js.ParseJson(text);
        }
    }
}
