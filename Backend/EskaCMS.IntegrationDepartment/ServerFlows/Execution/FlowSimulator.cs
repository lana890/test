using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.IntegrationDepartment.ServerFlows.Engine;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Execution
{
    /// <summary>One request of a simulated run: sent / skipped / notReached / responseError / error, with the body it would send and the outputs it would keep.</summary>
    public sealed record SimulatedRequest(string Group, string Request, string Result, string? Note, JToken? Body, JToken? Outputs);

    public sealed record SimulationResult(IReadOnlyList<SimulatedRequest> Requests, string? Reference, JToken? Context);

    /// <summary>
    /// Walks a flow for a context without calling anything: each request's body, whether it would
    /// run, and the outputs it would keep, using each request's server.sampleResponse in place of
    /// the real answer. Pure (no database, no clock of its own): the Admin's "Try it" builds the
    /// context exactly as the flow's trigger would. The flow's hidden values come back as "***".
    /// </summary>
    public static class FlowSimulator
    {
        public static SimulationResult Simulate(FlowDefinition definition, JObject context, DateTimeOffset now)
        {
            var data = new FlowRunData(context, new JObject(), definition, FlowRunData.OpsAt(now, definition.Settings.TimeZone));
            var hidden = definition.Settings.HiddenKeys;
            var report = new List<SimulatedRequest>();
            var stopped = false;

            void Add(string group, string request, string result, string? note, JToken? body, JToken? outputs) =>
                report.Add(new SimulatedRequest(group, request, result, note, StepPayload.Hide(body, hidden), StepPayload.Hide(outputs, hidden)));

            // As the runner: a condition that cannot be checked (its script or ops fail) stops the run.
            string? conditionFailure = null;
            bool Condition(JObject? list)
            {
                try
                {
                    return FlowConditions.Check(list, data.ConditionData(), data.Ops);
                }
                catch (Exception ex)
                {
                    conditionFailure = ex.Message;
                    return false;
                }
            }

            foreach (var group in definition.Groups)
            {
                var groupRuns = !stopped && Condition(group.ExecuteIf);
                if (conditionFailure != null)
                {
                    Add(group.Name, "condition", "error", $"The group's condition could not be checked: {conditionFailure}", null, null);
                    (stopped, conditionFailure) = (true, null);
                    groupRuns = false;
                }
                foreach (var request in group.Requests)
                {
                    if (stopped)
                    {
                        Add(group.Name, request.Name, "notReached", "An earlier request stopped the run.", null, null);
                        continue;
                    }
                    if (!groupRuns)
                    {
                        Add(group.Name, request.Name, "skipped", "The group's condition is false.", null, null);
                        continue;
                    }
                    if (!Condition(request.ExecuteIf))
                    {
                        if (conditionFailure != null)
                        {
                            Add(group.Name, request.Name, "error", $"The request's condition could not be checked: {conditionFailure}", null, null);
                            (stopped, conditionFailure) = (true, null);
                            continue;
                        }
                        Add(group.Name, request.Name, "skipped", "The request's condition is false.", null, null);
                        continue;
                    }

                    // A loop sends once per item of its list (the sample answers each time).
                    var items = request.Loop != null ? JsonPath.Get(data.Results, request.Loop.SourcePath) as JArray : null;
                    var rounds = items?.Count ?? 1;
                    var perItem = new JArray();
                    JToken? body = null;
                    JObject outputs = new();
                    string? note = null;
                    string? failed = null;
                    var givenUpItems = 0;
                    for (var i = 0; i < rounds && failed == null; i++)
                    {
                        if (items != null)
                            data.EnterItem(request.Loop!, items[i]!, i);
                        // As the runner: an item that fails is given up and the loop goes on, when the
                        // request has no error settings of its own (the browser catches it the same way).
                        var itemGoesOn = items != null && request.LoopItemFailuresContinue;
                        try
                        {
                            var built = FlowRunData.JsonSafe(new FlowMappingResolver(data).Prepare(request.DataMapping, request.StaticData, request.MergeData, request.LocalTree)) ?? new JObject();
                            body ??= built;

                            var sample = request.SampleResponse;
                            note = sample == null ? "No server.sampleResponse: outputs are empty." : null;
                            if (sample != null && request.ResponseChecks.Any(c => FlowConditions.Check(c, sample, data.Ops)))
                            {
                                if (!itemGoesOn)
                                {
                                    failed = "responseError";
                                    break;
                                }
                                var why = "The sample response matches a response check.";
                                perItem.Add(data.GivenUpOutputs(request, i, ref why));
                                givenUpItems++;
                                continue;
                            }

                            outputs = sample == null ? new JObject() : data.ReadOutputs(request.Outputs, data.Shape(request, sample));
                            var missing = request.Outputs.Keys.Where(k => outputs[k] == null).ToList();
                            if (sample != null && missing.Count > 0)
                                note = $"Not found in the sample response: {string.Join(", ", missing)}.";
                            perItem.Add(outputs);
                        }
                        catch (Exception ex)
                        {
                            var why = $"Could not build the request{(items != null ? $" for item {i + 1}" : "")}: {ex.Message}";
                            if (itemGoesOn)
                            {
                                perItem.Add(data.GivenUpOutputs(request, i, ref why));
                                givenUpItems++;
                            }
                            else
                            {
                                note = why;
                                failed = "error";
                            }
                        }
                        finally
                        {
                            if (items != null)
                                data.LeaveItem();
                        }
                    }

                    if (failed != null)
                    {
                        var why = failed == "responseError" ? "The sample response matches a response check (the call would count as failed)." : note!;
                        // As the runner: a call that may fail without stopping the run is given up, and
                        // what it answers instead (its fallback, else { error, name }) is what later calls read.
                        JObject? instead = null;
                        if (!request.Critical)
                        {
                            var reason = why;
                            instead = data.GivenUpOutputs(request, null, ref reason);
                            data.Store(request, instead);
                        }
                        Add(group.Name, request.Name, failed, why, body, instead);
                        stopped = request.Critical;
                        continue;
                    }

                    JToken result = items != null ? perItem : outputs;
                    if (items != null)
                        note = $"Sent once per item of \"{request.Loop!.SourcePath}\" ({items.Count}); the body shown is the first one's."
                            + (givenUpItems > 0 ? $" {givenUpItems} item(s) failed and were given up; the loop went on." : "")
                            + (note == null ? "" : " " + note);
                    if (request.Repeat != null)
                        note = $"Asked again until done ({request.Repeat.Strategy}, at most {request.Repeat.MaxAttempts} times); simulated once.{(note == null ? "" : " " + note)}";
                    data.Store(request, result);
                    Add(group.Name, request.Name, "sent", note, body, result);
                }

                if (!stopped && groupRuns && group.Transform != null)
                {
                    try
                    {
                        data.ApplyGroupTransform(group);
                    }
                    catch (Exception ex)
                    {
                        Add(group.Name, "transformResult", "error", $"The group's transformResult failed: {ex.Message}", null, null);
                        stopped = true;
                    }
                }
            }

            // the reference the run would show: orderReference, else the first value a page can read
            string? reference = null;
            if (definition.Reference is { } refFrom && data.OutputOf(refFrom) is { } r && !Js.IsNil(r))
                reference = Js.ToJsString(r);

            return new SimulationResult(report, reference, StepPayload.Hide(context, hidden));
        }
    }
}
