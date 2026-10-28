using System.Collections.Concurrent;
using System.Globalization;
using Jint;
using Jint.Native;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>A transform script that does not compile, throws, or goes past its limits.</summary>
    public sealed class FlowScriptException : Exception
    {
        public FlowScriptException(string message, Exception? inner = null) : base(message, inner) { }
    }

    /// <summary>
    /// The pages' JavaScript transforms, run on the server: the browser compiles each one as
    /// <c>new Function(arg, "return " + script)</c> (dynamic-api-caller.service.ts compileTransform),
    /// and so does this, in Jint, a JavaScript interpreter written in .NET.
    ///
    /// Sandbox: every call gets a fresh engine (disposed after it) with no access to .NET (Jint
    /// exposes nothing of the host unless asked), no file, network or clock other than JS Date, and
    /// hard limits on time, statements, memory, recursion and regular expressions. Values cross as JSON, so a script only ever sees and
    /// returns plain data. Dates use the flow's time zone, as the browser uses the device's.
    /// </summary>
    public static class JsScript
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);
        private const int MaxStatements = 200_000;
        private const long MaxMemoryBytes = 32L * 1024 * 1024;
        private const int MaxRecursion = 256;

        private static readonly ConcurrentDictionary<string, Prepared<Acornima.Ast.Script>> Compiled = new();
        private static readonly Prepared<Acornima.Ast.Script> Call =
            Jint.Engine.PrepareScript("(function(){ var v = __in === undefined ? undefined : JSON.parse(__in); var r = __fn(v); return r === undefined ? undefined : JSON.stringify(r); })()");

        private static readonly JsonSerializerSettings ReadSettings = new() { DateParseHandling = DateParseHandling.None, MaxDepth = 128 };

        /// <summary>Compiles the script as the browser would, or throws a <see cref="FlowScriptException"/> saying why not.</summary>
        public static void Check(string argName, string script)
        {
            _ = Prepare(argName, script);
        }

        /// <summary>
        /// Runs <c>(function(argName) { return script })(value)</c>. Null in and out stands for JS
        /// undefined; JSON null is a <see cref="JValue"/> null.
        /// </summary>
        public static JToken? Run(string argName, string script, JToken? value, TimeZoneInfo timeZone)
        {
            var function = Prepare(argName, script);
            try
            {
                using var engine = new Jint.Engine(options =>
                {
                    options.TimeoutInterval(Timeout);
                    options.MaxStatements(MaxStatements);
                    options.LimitMemory(MaxMemoryBytes);
                    options.LimitRecursion(MaxRecursion);
                    // a script's regular expression is held to the script's own time limit
                    options.RegexTimeoutInterval(Timeout);
                    options.LocalTimeZone(timeZone);
                    options.Culture(CultureInfo.InvariantCulture);
                });
                engine.SetValue("__in", value == null ? JsValue.Undefined : new JsString(value.ToString(Formatting.None)));
                engine.SetValue("__fn", engine.Evaluate(function));
                var result = engine.Evaluate(Call);
                if (result.IsUndefined())
                    return null;
                return JsonConvert.DeserializeObject<JToken>(result.AsString(), ReadSettings) ?? JValue.CreateNull();
            }
            catch (FlowScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FlowScriptException($"The script failed: {Describe(ex)}", ex);
            }
        }

        private static Prepared<Acornima.Ast.Script> Prepare(string argName, string script)
        {
            var key = argName + "\u0000" + script;
            if (Compiled.TryGetValue(key, out var ready))
                return ready;
            try
            {
                // new Function(arg, "return " + script): the same text, so the same automatic
                // semicolon rules (a script that starts on a new line returns undefined there too)
                var prepared = Jint.Engine.PrepareScript("(function(" + argName + ") {\nreturn " + script + "\n})");
                if (Compiled.Count < 2000)
                    Compiled[key] = prepared;
                return prepared;
            }
            catch (Exception ex)
            {
                throw new FlowScriptException($"The script does not compile: {Describe(ex)}", ex);
            }
        }

        private static string Describe(Exception ex) => ex switch
        {
            TimeoutException => $"it ran longer than {Timeout.TotalSeconds:0} s",
            Jint.Runtime.StatementsCountOverflowException => $"it ran more than {MaxStatements} statements",
            Jint.Runtime.MemoryLimitExceededException => $"it used more than {MaxMemoryBytes / (1024 * 1024)} MB",
            Jint.Runtime.RecursionDepthOverflowException => "it called itself too deeply",
            _ => ex.Message,
        };
    }
}
