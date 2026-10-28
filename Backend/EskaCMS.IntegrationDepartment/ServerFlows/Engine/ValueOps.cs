using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// value-ops.ts <c>applyValueOps</c>: the declarative <c>valueOps</c> pipeline a
    /// mapping, response or condition carries, run with the browser's semantics.
    /// </summary>
    /// <remarks>
    /// Ops run left to right, each taking what the last returned. A step that throws
    /// is logged and skipped (the value carries on unchanged); an unknown op is logged
    /// and skipped; on a null/undefined value every op outside RUNS_ON_NIL is skipped.
    /// Browser-only ops are never executed: <see cref="Validate"/> rejects them, and
    /// <see cref="Apply"/> skips them (logged) if they get through.
    /// </remarks>
    public static class ValueOps
    {
        /// <summary>Nested pipelines (map/switch/when/use) deeper than this throw, instead of overflowing the stack.</summary>
        private const int MaxDepth = 32;

        private static readonly JObject NoArgs = new JObject();

        private static readonly Dictionary<string, ValueOpFunctions.OpFn> Ops = new Dictionary<string, ValueOpFunctions.OpFn>(StringComparer.Ordinal)
        {
            ["text"] = ValueOpFunctions.Text,
            ["trim"] = ValueOpFunctions.Trim,
            ["upper"] = ValueOpFunctions.Upper,
            ["lower"] = ValueOpFunctions.Lower,
            ["capitalize"] = ValueOpFunctions.Capitalize,
            ["replace"] = ValueOpFunctions.Replace,
            ["stripSpaces"] = ValueOpFunctions.StripSpaces,
            ["latinDigits"] = ValueOpFunctions.LatinDigits,
            ["arabicDigits"] = ValueOpFunctions.ArabicDigits,
            ["mask"] = ValueOpFunctions.Mask,
            ["keepOnly"] = ValueOpFunctions.KeepOnly,
            ["split"] = ValueOpFunctions.Split,
            ["slice"] = ValueOpFunctions.Slice,
            ["pad"] = ValueOpFunctions.Pad,
            ["template"] = ValueOpFunctions.Template,
            ["escapeHtml"] = ValueOpFunctions.EscapeHtml,
            ["newlinesToHtml"] = ValueOpFunctions.NewlinesToHtml,
            ["urlSafe"] = ValueOpFunctions.UrlSafe,
            ["number"] = ValueOpFunctions.Number,
            ["round"] = ValueOpFunctions.Round,
            ["math"] = ValueOpFunctions.MathOp,
            ["clamp"] = ValueOpFunctions.Clamp,
            ["abs"] = ValueOpFunctions.Abs,
            ["date"] = ValueOpFunctions.Date,
            ["dateAdd"] = ValueOpFunctions.DateAdd,
            ["now"] = ValueOpFunctions.Now,
            ["parseDate"] = ValueOpFunctions.ParseDate,
            ["hijri"] = ValueOpFunctions.Hijri,
            ["gregorian"] = ValueOpFunctions.Gregorian,
            ["json"] = ValueOpFunctions.Json,
            ["parse"] = ValueOpFunctions.Parse,
            ["get"] = ValueOpFunctions.Get,
            ["set"] = ValueOpFunctions.Set,
            ["pick"] = ValueOpFunctions.Pick,
            ["omit"] = ValueOpFunctions.Omit,
            ["rename"] = ValueOpFunctions.Rename,
            ["merge"] = ValueOpFunctions.Merge,
            ["defaults"] = ValueOpFunctions.Defaults,
            ["keys"] = ValueOpFunctions.Keys,
            ["values"] = ValueOpFunctions.Values,
            ["entries"] = ValueOpFunctions.EntriesOp,
            ["toArray"] = ValueOpFunctions.ToArray,
            ["map"] = ValueOpFunctions.Map,
            ["filter"] = ValueOpFunctions.Filter,
            ["find"] = ValueOpFunctions.Find,
            ["first"] = ValueOpFunctions.First,
            ["last"] = ValueOpFunctions.Last,
            ["at"] = ValueOpFunctions.At,
            ["compact"] = ValueOpFunctions.Compact,
            ["unique"] = ValueOpFunctions.Unique,
            ["flatten"] = ValueOpFunctions.Flatten,
            ["sort"] = ValueOpFunctions.Sort,
            ["join"] = ValueOpFunctions.Join,
            ["sum"] = ValueOpFunctions.Sum,
            ["count"] = ValueOpFunctions.Count,
            ["limit"] = ValueOpFunctions.Limit,
            ["groupBy"] = ValueOpFunctions.GroupBy,
            ["default"] = ValueOpFunctions.Default,
            ["coalesce"] = ValueOpFunctions.Coalesce,
            ["lookup"] = ValueOpFunctions.Lookup,
            ["switch"] = ValueOpFunctions.Switch,
            ["when"] = ValueOpFunctions.When,
            ["boolean"] = ValueOpFunctions.Boolean,
            ["exists"] = ValueOpFunctions.Exists,
            ["isEmpty"] = ValueOpFunctions.IsEmptyOp,
            ["constant"] = ValueOpFunctions.Constant,
            ["script"] = ValueOpFunctions.Script,
        };

        private static readonly HashSet<string> BrowserOnlySet = new HashSet<string>(StringComparer.Ordinal)
        {
            // files: need File/atob, or exist to feed multipart uploads built in the browser
            "toFile", "dataUrl", "fileName", "fileSize",
            // device and place: navigator / Intl of the visitor's device
            "deviceInfo", "timeZone", "regionName", "localTime",
            // ("script" runs on the server since 2026-10-13, in the JsScript sandbox)
        };

        /// <summary>value-ops.ts RUNS_ON_NIL: ops that still run when the value is null/undefined.</summary>
        private static readonly HashSet<string> RunsOnNil = new HashSet<string>(StringComparer.Ordinal)
        {
            "default", "coalesce", "json", "exists", "isEmpty", "switch", "script", "set", "constant",
            "deviceInfo", "timeZone", "localTime",
        };

        private static readonly HashSet<string> NamedKeepOnlySets = new HashSet<string>(StringComparer.Ordinal) { "digits", "letters", "alphanumeric", "arabic" };

        /// <summary>The ops a server flow can run (the pipeline directives <c>use</c>/<c>ref</c> are also accepted).</summary>
        public static IReadOnlyCollection<string> SupportedOps { get; } = Array.AsReadOnly(Ops.Keys.ToArray());

        /// <summary>Ops the browser runtime has that a server flow rejects.</summary>
        public static IReadOnlyCollection<string> BrowserOnlyOps { get; } = Array.AsReadOnly(BrowserOnlySet.ToArray());

        /// <summary>
        /// Runs <paramref name="ops"/> (a JArray of op names and <c>{ "op": … }</c> objects) over
        /// <paramref name="value"/>. A null/empty/non-array <paramref name="ops"/> returns the value
        /// unchanged. Neither input is mutated; the result may be one of their tokens (for
        /// example <c>get</c> returns a node of the value) — clone it before mutating it.
        /// </summary>
        public static JToken? Apply(JToken? value, JToken? ops, ValueOpsContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            return ApplyCore(value, ops, ctx, 0);
        }

        internal static JToken? ApplyCore(JToken? value, JToken? ops, ValueOpsContext ctx, int depth)
        {
            if (!(ops is JArray list) || list.Count == 0) return value;
            // JS would recurse until a RangeError; it propagates like one (an enclosing op catches it).
            if (depth > MaxDepth) throw new InvalidOperationException("valueOps nested more than " + MaxDepth + " levels deep");

            JToken? current = value;
            foreach (JToken entry in list)
            {
                JObject step;
                JToken? opToken;
                if (Js.TypeOf(entry) == JsType.String)
                {
                    step = NoArgs;
                    opToken = entry;
                }
                else if (entry is JObject o)
                {
                    step = o;
                    opToken = JsObjects.Norm(o["op"]);
                }
                else
                {
                    continue; // `!step || !step.op`: numbers, arrays, null are skipped silently
                }
                if (!Js.Truthy(opToken)) continue;

                bool opIsString = Js.TypeOf(opToken) == JsType.String;
                string opName = opIsString ? Js.StringValue(opToken!) : Js.ToJsString(opToken);

                if (opIsString && (opName == "use" || opName == "ref"))
                {
                    current = ApplyNamedPipeline(current, step, ctx, depth);
                    continue;
                }
                if (BrowserOnlySet.Contains(opName))
                {
                    ctx.Log?.Invoke("[valueOps] browser-only op \"" + opName + "\" skipped on the server");
                    continue;
                }
                if (!Ops.TryGetValue(opName, out ValueOpFunctions.OpFn? fn))
                {
                    ctx.Log?.Invoke("[valueOps] unknown op \"" + opName + "\"");
                    continue;
                }
                if (Js.IsNil(current) && !(opIsString && RunsOnNil.Contains(opName))) continue;

                try
                {
                    current = fn(current, step, ctx, depth);
                }
                catch (Exception e)
                {
                    ctx.Log?.Invoke("[valueOps] \"" + opName + "\" failed: " + e.Message);
                }
            }
            return current;
        }

        private static JToken? ApplyNamedPipeline(JToken? current, JObject step, ValueOpsContext ctx, int depth)
        {
            JToken? name = JsObjects.Norm(step["name"]);
            JToken? resolved = ctx.ResolveRef != null && Js.Truthy(name) ? ctx.ResolveRef(Js.ToJsString(name)) : null;
            if (resolved is JArray pipeline) return ApplyCore(current, pipeline, ctx, depth + 1);
            if (Js.TypeOf(resolved) == JsType.String)
            {
                // Server port: a raw-JS library entry is the `script` op in disguise; it is
                // skipped, which is what the browser does when such a script throws.
                ctx.Log?.Invoke("[valueOps] pipeline \"" + Js.ToJsString(name) + "\" is a script and cannot run on the server");
                return current;
            }
            ctx.Log?.Invoke("[valueOps] no pipeline named \"" + Js.ToJsString(name) + "\"");
            return current;
        }

        /// <summary>
        /// Problems that make <paramref name="ops"/> unfit for a server flow, as readable
        /// sentences; empty means it can run. Checked: the list shape, each step's shape,
        /// unknown and browser-only ops (also inside map/switch/when), regexes that do not
        /// compile, and arguments whose absence or type makes the browser misbehave
        /// (e.g. <c>set</c> without <c>path</c>, <c>template</c> without <c>pattern</c>,
        /// predicates that are not objects). A missing value (null) is fine.
        /// </summary>
        public static IReadOnlyList<string> Validate(JToken? ops)
        {
            var problems = new List<string>();
            ValidateList(ops, "valueOps", problems, 0);
            return problems;
        }

        private static void ValidateList(JToken? ops, string where, List<string> problems, int depth)
        {
            if (Js.IsNil(ops)) return;
            if (depth > MaxDepth)
            {
                problems.Add(where + ": pipelines nested more than " + MaxDepth + " levels deep.");
                return;
            }
            if (!(ops is JArray list))
            {
                problems.Add(where + " must be an array of ops; the runtime ignores anything else.");
                return;
            }
            for (int i = 0; i < list.Count; i++)
            {
                JToken entry = list[i];
                string label = where + "[" + i + "]";
                string name;
                JObject? step = null;
                if (Js.TypeOf(entry) == JsType.String)
                {
                    name = Js.StringValue(entry);
                    if (name.Length == 0)
                    {
                        problems.Add(label + ": an empty op name (the step is skipped).");
                        continue;
                    }
                }
                else if (entry is JObject o)
                {
                    step = o;
                    JToken? op = JsObjects.Norm(o["op"]);
                    if (Js.TypeOf(op) != JsType.String || Js.StringValue(op!).Length == 0)
                    {
                        problems.Add(label + ": needs an \"op\" name (the step is skipped).");
                        continue;
                    }
                    name = Js.StringValue(op!);
                }
                else
                {
                    problems.Add(label + ": must be an op name or an object with \"op\" (the step is skipped).");
                    continue;
                }

                label += " (\"" + name + "\")";
                if (name == "use" || name == "ref")
                {
                    JToken? refName = step == null ? null : JsObjects.Norm(step["name"]);
                    if (Js.TypeOf(refName) != JsType.String || Js.StringValue(refName!).Length == 0)
                    {
                        problems.Add(label + ": needs the \"name\" of a pipeline in the transform library.");
                    }
                    continue;
                }
                if (BrowserOnlySet.Contains(name))
                {
                    problems.Add(label + ": browser-only op, it cannot run in a server flow.");
                    continue;
                }
                if (!Ops.ContainsKey(name))
                {
                    problems.Add(label + ": unknown op (the runtime skips it).");
                    continue;
                }
                if (step != null) ValidateArgs(name, step, label, problems, depth);
            }
        }

        private static void ValidateArgs(string name, JObject a, string label, List<string> problems, int depth)
        {
            JToken? Arg(string key) => JsObjects.Norm(a[key]);
            bool Present(string key) => !Js.IsNil(Arg(key));
            void RequireStringIfPresent(string key)
            {
                if (Present(key) && Js.TypeOf(Arg(key)) != JsType.String) problems.Add(label + ": \"" + key + "\" must be a string (the step throws in the browser).");
            }
            void RequireArrayIfPresent(string key)
            {
                if (Present(key) && !(Arg(key) is JArray)) problems.Add(label + ": \"" + key + "\" must be an array (the step throws in the browser).");
            }
            void CheckRegex(string pattern, string key, string consequence)
            {
                try
                {
                    JsRegex.Get(pattern);
                }
                catch (ArgumentException e)
                {
                    problems.Add(label + ": \"" + key + "\" is not a valid regular expression (" + e.Message + "); " + consequence);
                }
            }

            switch (name)
            {
                case "replace":
                    if (Js.Truthy(Arg("regex")) && Present("find")) CheckRegex(Js.ToJsString(Arg("find")), "find", "the browser leaves the text unchanged.");
                    break;
                case "keepOnly":
                    if (Present("characters") && !NamedKeepOnlySets.Contains(Js.ToJsString(Arg("characters"))))
                    {
                        CheckRegex("[^" + Js.ToJsString(Arg("characters")) + "]", "characters", "the browser leaves the text unchanged.");
                    }
                    break;
                case "urlSafe":
                    if (Js.Truthy(Arg("chars"))) CheckRegex("[" + JsStrings.EscapeRegex(Js.ToJsString(Arg("chars"))) + "]", "chars", "the step fails.");
                    break;
                case "template":
                    if (Js.TypeOf(Arg("pattern")) != JsType.String) problems.Add(label + ": needs a string \"pattern\" (without one the result is an empty string).");
                    break;
                case "set":
                    if (Js.TypeOf(Arg("path")) != JsType.String || Js.StringValue(Arg("path")!).Length == 0)
                    {
                        problems.Add(label + ": needs a \"path\" (without one it writes a key named \"undefined\").");
                    }
                    break;
                case "date":
                    RequireStringIfPresent("format");
                    break;
                case "parseDate":
                    RequireStringIfPresent("pattern");
                    break;
                case "hijri":
                    if (Js.Truthy(Arg("parse"))) RequireStringIfPresent("parse");
                    RequireStringIfPresent("format");
                    break;
                case "gregorian":
                    RequireStringIfPresent("parse");
                    if (Js.Truthy(Arg("format"))) RequireStringIfPresent("format");
                    break;
                case "pick":
                case "omit":
                    RequireArrayIfPresent("fields");
                    break;
                case "rename":
                    if (Present("fields") && !(Arg("fields") is JObject)) problems.Add(label + ": \"fields\" must be an object of { newName: \"old.path\" }.");
                    break;
                case "lookup":
                    if (Present("table") && !(Arg("table") is JObject)) problems.Add(label + ": \"table\" must be an object.");
                    break;
                case "coalesce":
                    RequireArrayIfPresent("then");
                    break;
                case "map":
                    if (Js.Truthy(Arg("ops"))) ValidateList(Arg("ops"), label + ".ops", problems, depth + 1);
                    break;
                case "filter":
                case "find":
                    ValidatePredicate(Arg("where"), label + ".where", problems);
                    break;
                case "when":
                    ValidatePredicate(Arg("is"), label + ".is", problems);
                    ValidateList(Arg("then"), label + ".then", problems, depth + 1);
                    break;
                case "switch":
                    {
                        JToken? cases = Arg("cases");
                        if (Present("cases") && !(cases is JArray)) problems.Add(label + ": \"cases\" must be an array.");
                        if (cases is JArray caseList)
                        {
                            for (int i = 0; i < caseList.Count; i++)
                            {
                                string caseLabel = label + ".cases[" + i + "]";
                                if (!(caseList[i] is JObject branch))
                                {
                                    problems.Add(caseLabel + ": must be an object { when, then | ops }.");
                                    continue;
                                }
                                ValidatePredicate(JsObjects.Norm(branch["when"]), caseLabel + ".when", problems);
                                JToken? branchOps = JsObjects.Norm(branch["ops"]);
                                if (Js.Truthy(branchOps)) ValidateList(branchOps, caseLabel + ".ops", problems, depth + 1);
                            }
                        }
                        if (Js.Truthy(Arg("elseOps"))) ValidateList(Arg("elseOps"), label + ".elseOps", problems, depth + 1);
                        break;
                    }
            }
        }

        private static void ValidatePredicate(JToken? predicate, string where, List<string> problems)
        {
            if (!Js.Truthy(predicate)) return;
            if (!(predicate is JObject p))
            {
                if (!(predicate is JArray)) problems.Add(where + ": must be an object such as { \"path\": ..., \"equals\": ... } (a " + Js.TypeOf(predicate).ToString().ToLowerInvariant() + " throws in the browser).");
                return;
            }
            JToken? matches = JsObjects.Norm(p["matches"]);
            if (p.ContainsKey("matches"))
            {
                try
                {
                    JsRegex.Get(Js.ToJsString(matches));
                }
                catch (ArgumentException e)
                {
                    problems.Add(where + ": \"matches\" is not a valid regular expression (" + e.Message + "); the test is always false.");
                }
            }
            if (p.ContainsKey("in"))
            {
                JToken? list = JsObjects.Norm(p["in"]);
                if (!Js.IsNil(list) && !(list is JArray) && Js.TypeOf(list) != JsType.String)
                {
                    problems.Add(where + ": \"in\" must be an array (anything else throws in the browser).");
                }
            }
        }
    }
}
