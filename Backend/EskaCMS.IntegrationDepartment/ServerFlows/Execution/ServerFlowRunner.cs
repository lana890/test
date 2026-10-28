using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.ServerFlows;
using EskaCMS.Infrastructure.Data;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.IntegrationDepartment.ServerFlows.Engine;
using EskaCMS.IntegrationDepartment.ServerFlows.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Execution
{
    /// <summary>
    /// Runs one run, from wherever it stopped.
    ///
    /// How it stays safe:
    ///  - Lease: a run is claimed with one conditional UPDATE; every later write to it repeats the
    ///    owner check, so two workers (or two servers) can never run it at once. The lease is renewed
    ///    while a call is out, however long it takes; a crashed worker's lease simply expires and the
    ///    next sweep resumes the run.
    ///  - Steps: a "Calling" row is saved BEFORE each call. After a crash that row tells the next
    ///    worker the call may have gone out.
    ///  - Classes: read / idempotentWrite calls are retried. A nonIdempotent call (e.g. issuing the
    ///    policy) is never re-sent blindly: when its result is unclear the guard asks the other side;
    ///    "not done" allows ONE more send, anything else waits for a person.
    ///  - Outputs: only the declared outputs feed the next requests (on the run and the step), so a
    ///    resumed run has exactly what the first try had; a pickup takes from the steps whatever the
    ///    run had not kept yet. Each attempt also keeps the body it sent and the answer it got
    ///    (secrets and hidden values masked, capped) for the admin page, and every error text is
    ///    built from that masked answer.
    ///  - Edited bodies: an admin can give a request the exact body to send next
    ///    (run.RequestOverrides); it is used until that request succeeds.
    /// </summary>
    public sealed class ServerFlowRunner
    {
        /// <summary>
        /// How the step of a non-critical request that failed for good begins. Such a request can be
        /// sent again on its own later (admin "Send again"); nothing else of the run is repeated.
        /// </summary>
        internal const string GivenUp = "Given up (not critical)";

        /// <summary>How the step of a call whose answer said it failed (a refusal, not a time-out) begins.</summary>
        internal const string RefusalPrefix = "The response says it failed";

        private const string KindCall = "call";

        /// <summary>A polling attempt whose answer was "not done yet" (repeatConfig): it settles nothing.</summary>
        private const string KindPoll = "poll";

        private const string GroupConditionFalse = "The group's condition is false.";
        private const string KindGuard = "guard";
        private const string KindManual = "manual";
        private const string Stopping = "The server was stopping; the run continues on the next start.";

        /// <summary>One polling wait up to this long happens inside the pickup...</summary>
        private const int ShortWaitMs = 15_000;

        /// <summary>...and all of them together up to this long, so one run never holds a worker for long; past that the run is released.</summary>
        private const int MaxPickupWaitMs = 60_000;

        private readonly IServiceScopeFactory _scopes;
        private readonly IOptionsMonitor<ServerFlowOptions> _options;
        private readonly ServerFlowCatalog _catalog;
        private readonly ServerFlowSignals _signals;
        private readonly ServerFlowQueue _queue;
        private readonly FlowCaller _caller;
        private readonly ServerFlowAlerts _alerts;
        private readonly ILogger<ServerFlowRunner> _logger;

        public ServerFlowRunner(
            IServiceScopeFactory scopes,
            IOptionsMonitor<ServerFlowOptions> options,
            ServerFlowCatalog catalog,
            ServerFlowSignals signals,
            ServerFlowQueue queue,
            FlowCaller caller,
            ServerFlowAlerts alerts,
            ILogger<ServerFlowRunner> logger)
        {
            _scopes = scopes;
            _options = options;
            _catalog = catalog;
            _signals = signals;
            _queue = queue;
            _caller = caller;
            _alerts = alerts;
            _logger = logger;
        }

        public async Task RunAsync(long runId, string workerId, CancellationToken stopping)
        {
            var options = _options.CurrentValue;
            if (!options.Enabled)
                return;

            using var scope = _scopes.CreateScope();
            var sp = scope.ServiceProvider;
            var runs = sp.GetRequiredService<IRepository<ServerFlowRun>>();

            var now = DateTimeOffset.UtcNow;
            var leaseUntil = now.AddSeconds(options.LeaseSeconds);
            var claimed = await runs.Query()
                .Where(r => r.Id == runId
                    && (r.Status == ServerFlowRunStatus.Queued || r.Status == ServerFlowRunStatus.Waiting || r.Status == ServerFlowRunStatus.Running)
                    && (r.NextAttemptAt == null || r.NextAttemptAt <= now)
                    && (r.LeaseUntil == null || r.LeaseUntil < now))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, ServerFlowRunStatus.Running)
                    .SetProperty(r => r.LeaseOwner, workerId)
                    .SetProperty(r => r.LeaseUntil, leaseUntil)
                    .SetProperty(r => r.Attempts, r => r.Attempts + 1)
                    .SetProperty(r => r.UpdatedOn, now), CancellationToken.None);
            if (claimed == 0)
                return; // not due, finished, or another worker has it

            var run = await runs.Query().AsNoTracking().FirstAsync(r => r.Id == runId);
            var session = new RunSession(this, sp, run, workerId, options, stopping);
            try
            {
                await session.ExecuteAsync();
            }
            catch (LeaseLostException)
            {
                _logger.LogWarning("Server flow run {RunId}: lease lost, another worker continues it.", runId);
            }
            catch (Exception ex)
            {
                // A fault in the runner itself (not in a call): try again later, then ask a person.
                _logger.LogError(ex, "Server flow run {RunId} failed unexpectedly.", runId);
                await session.FailUnexpectedlyAsync(ex);
            }
            finally
            {
                _signals.Pulse(run.SubjectId);
            }
        }

        private sealed class LeaseLostException : Exception { }

        private enum Next
        {
            Continue,
            Stop,
        }

        /// <summary>Everything one pickup of one run needs.</summary>
        private sealed class RunSession
        {
            private readonly ServerFlowRunner _owner;
            private readonly ServerFlowRun _run;
            private readonly string _workerId;
            private readonly ServerFlowOptions _options;
            private readonly CancellationToken _stopping;
            private readonly IRepository<ServerFlowRun> _runs;
            private readonly IRepository<ServerFlowStep> _steps;
            private readonly IRepository<ServerFlow> _flows;
            private readonly IdcmsLogService _log;

            private FlowDefinition? _definition;

            /// <summary>The flow's hidden values (settings.privacy.hide): masked in the bodies and answers a step keeps.</summary>
            private IReadOnlyCollection<string>? Hidden => _definition?.Settings.HiddenKeys;

            private JObject _overrides = new JObject();
            private FlowRunData _data = null!;
            private FlowIdentity _who = null!;

            /// <summary>The run's steps by step name ("loadQuote", "loadQuote[3]"), and the requests that have any.</summary>
            private readonly Dictionary<string, List<ServerFlowStep>> _stepsByName = new(StringComparer.Ordinal);
            private readonly HashSet<string> _started = new(StringComparer.Ordinal);

            /// <summary>What the run row holds now (outputs, reference, last error), so it is written only when it changes.</summary>
            private string? _savedOutputs;
            private string? _reference;
            private string? _lastError;

            /// <summary>How long this pickup has waited between polling attempts.</summary>
            private int _waitedMs;

            public RunSession(ServerFlowRunner owner, IServiceProvider sp, ServerFlowRun run, string workerId, ServerFlowOptions options, CancellationToken stopping)
            {
                _owner = owner;
                _run = run;
                _workerId = workerId;
                _options = options;
                _stopping = stopping;
                _runs = sp.GetRequiredService<IRepository<ServerFlowRun>>();
                _steps = sp.GetRequiredService<IRepository<ServerFlowStep>>();
                _flows = sp.GetRequiredService<IRepository<ServerFlow>>();
                _log = sp.GetRequiredService<IdcmsLogService>();
                _savedOutputs = run.Outputs;
                _reference = run.Reference;
                _lastError = run.LastError;
            }

            public async Task ExecuteAsync()
            {
                var flow = await _flows.Query().AsNoTracking().FirstOrDefaultAsync(f => f.Id == _run.FlowId);
                if (flow == null)
                {
                    await NeedsAttentionAsync($"Flow version {_run.FlowId} no longer exists.", null);
                    return;
                }

                var parsed = _owner._catalog.Parse(flow);
                if (parsed.Definition == null)
                {
                    var first = parsed.Issues.FirstOrDefault(i => i.Severity == FlowIssueSeverity.Error);
                    await NeedsAttentionAsync($"Flow \"{flow.Name}\" v{flow.Version} does not pass the checks any more: {first?.Message}", null);
                    return;
                }
                var definition = _definition = parsed.Definition;

                // Only pickups that ended unplanned count (a fault, a stop without a goodbye): planned
                // waits (retries, polling, a guard's settle time) give their count back (ReleaseAsync).
                if (_run.Attempts > _options.MaxRunPickups)
                {
                    await NeedsAttentionAsync($"The run stopped unexpectedly {_run.Attempts - 1} times without finishing (an error, or the server stopping during it).", _run.LastError);
                    return;
                }

                var context = ParseObject(_run.Context);
                var outputs = ParseObject(_run.Outputs);
                _overrides = ParseObject(_run.RequestOverrides);
                _data = new FlowRunData(context, outputs, definition, FlowRunData.OpsAt(DateTimeOffset.UtcNow, definition.Settings.TimeZone));
                _who = IdentityOf(context);

                foreach (var step in await _steps.Query().Where(s => s.RunId == _run.Id).OrderBy(s => s.Id).ToListAsync())
                    Index(step);
                await RestoreOutputsAsync();

                foreach (var group in definition.Groups)
                {
                    if (_stopping.IsCancellationRequested)
                    {
                        await ReleaseAsync(ServerFlowRunStatus.Waiting, DateTimeOffset.UtcNow, Stopping);
                        return;
                    }

                    if (group.Requests.All(r => IsSettled(r.Name)))
                    {
                        // The amount check is not a step of its own, so a resumed run (Try again,
                        // a new pickup) checks it again: a stop for a wrong amount stays a stop.
                        foreach (var request in group.Requests)
                            if (await CheckPaymentAsync(request) == Next.Stop)
                                return;
                        // Rebuilt from the kept outputs on every pickup (it is not stored).
                        if (!GroupSkipped(group) && await TransformGroupAsync(group) == Next.Stop)
                            return;
                        continue;
                    }

                    // A group already under way passed its condition before; the data it reads does
                    // not change, so only a group that has not started is asked again.
                    if (!group.Requests.Any(r => _started.Contains(r.Name)))
                    {
                        var runs = await ConditionAsync(group.ExecuteIf, $"the condition of group \"{group.Name}\"");
                        if (runs == null)
                            return;
                        if (runs == false)
                        {
                            foreach (var request in group.Requests)
                                await AddStepAsync(request, KindCall, ServerFlowStepStatus.Skipped, GroupConditionFalse, null);
                            continue;
                        }
                    }

                    foreach (var request in group.Requests)
                    {
                        if (await RunRequestAsync(request) == Next.Stop)
                            return;
                    }

                    if (await TransformGroupAsync(group) == Next.Stop)
                        return;
                }

                await SucceedAsync();
            }

            /// <summary>
            /// Every settled request's outputs are on its last step. A pickup after a stop between a
            /// step's write and the run's finds them missing on the run: they are taken from the steps
            /// and kept on the run before anything reads them.
            /// </summary>
            private async Task RestoreOutputsAsync()
            {
                foreach (var request in _definition!.Requests.Where(r => _data.Results[r.Name] == null))
                {
                    var last = StepsOf(request.Name).LastOrDefault();
                    var kept = ParseToken(last?.Outputs);
                    if (last?.Status == ServerFlowStepStatus.Succeeded)
                        _data.Store(request, kept ?? new JObject());
                    else if (last?.Status == ServerFlowStepStatus.Skipped && kept != null)
                        _data.Store(request, kept); // what a call that was given up answers instead
                }
                await SaveProgressAsync();
            }

            #region One request

            private async Task<Next> RunRequestAsync(FlowRequest request)
            {
                var last = StepsOf(request.Name).LastOrDefault();

                if (last != null)
                {
                    // Already done (call, guard, a person's decision, or given up): its outputs were
                    // restored at the pickup; the amount check runs again on every resume (it is not a step).
                    if (last.Status is ServerFlowStepStatus.Succeeded or ServerFlowStepStatus.Skipped)
                        return await CheckPaymentAsync(request);

                    if (request.Class == FlowRequestClass.NonIdempotent)
                    {
                        // Sent, outcome unknown (crashed mid-call, or the answer was unclear): ask first.
                        if (last.Kind == KindCall && last.Status is ServerFlowStepStatus.Calling or ServerFlowStepStatus.Unclear)
                            return await GuardAsync(request);
                        if (last.Kind == KindGuard && last.Status is ServerFlowStepStatus.Calling or ServerFlowStepStatus.Unclear)
                            return await GuardAsync(request);
                        // guard "not done", a person's "did not go through", or a refused call: may send.
                    }
                    else if (last.Kind == KindCall && last.Status == ServerFlowStepStatus.Calling)
                    {
                        // A read or a repeatable write that a crash interrupted: count it as a failure.
                        await InterruptedAsync(last);
                    }
                }
                else if (!_started.Contains(request.Name))
                {
                    var runs = await ConditionAsync(request.ExecuteIf, $"the condition of \"{request.Name}\"");
                    if (runs == null)
                        return Next.Stop;
                    if (runs == false)
                    {
                        await AddStepAsync(request, KindCall, ServerFlowStepStatus.Skipped, "The request's condition is false.", null);
                        return Next.Continue;
                    }
                }

                if (request.Loop != null)
                    return await RunLoopAsync(request);

                return await SendAsync(Target.Of(request));
            }

            /// <summary>
            /// loopOn / a conditional group's list: one call per item, each a step of its own
            /// ("name[0]", "name[1]", ...), so a resumed run goes on from the item it stopped at. The
            /// request's result is the list of the items' outputs, kept as one step on the request.
            /// </summary>
            private async Task<Next> RunLoopAsync(FlowRequest request)
            {
                var loop = request.Loop!;
                if (JsonPath.Get(_data.Results, loop.SourcePath) is not JArray items)
                    return await SendAsync(Target.Of(request)); // as the browser: one call, no item

                var results = new JArray();
                for (var i = 0; i < items.Count; i++)
                {
                    var target = new Target(request, $"{request.Name}[{i}]", i);
                    var last = StepsOf(target.StepName).LastOrDefault();
                    if (last != null && last.Status is ServerFlowStepStatus.Succeeded or ServerFlowStepStatus.Skipped)
                    {
                        results.Add(ParseToken(last.Outputs) ?? new JObject());
                        continue;
                    }

                    // A long list stops between two items when the server stops, not after the last one.
                    if (_stopping.IsCancellationRequested)
                    {
                        await ReleaseAsync(ServerFlowRunStatus.Waiting, DateTimeOffset.UtcNow, Stopping);
                        return Next.Stop;
                    }
                    if (last is { Kind: KindCall, Status: ServerFlowStepStatus.Calling })
                        await InterruptedAsync(last);

                    _data.EnterItem(loop, items[i]!, i);
                    Next next;
                    try
                    {
                        next = await SendAsync(target);
                    }
                    finally
                    {
                        _data.LeaveItem();
                    }
                    if (next == Next.Stop)
                        return Next.Stop;
                    results.Add(ParseToken(StepsOf(target.StepName).Last().Outputs) ?? new JObject());
                }

                await AddStepAsync(request, KindCall, ServerFlowStepStatus.Succeeded, items.Count == 0 ? "The list was empty." : $"{items.Count} item(s) done.", results);
                await KeepOutputsAsync(request, results, succeeded: true);
                return Next.Continue;
            }

            /// <summary>A request, or one item of a request that loops (its own steps and retries).</summary>
            private sealed record Target(FlowRequest Request, string StepName, int? Index)
            {
                public static Target Of(FlowRequest request) => new(request, request.Name, null);

                public string Label => Index == null ? $"\"{Request.Name}\"" : $"\"{Request.Name}\" (item {Index + 1} of the list)";
            }

            private async Task<Next> SendAsync(Target target)
            {
                var request = target.Request;
                if (request.Class == FlowRequestClass.NonIdempotent && UnclearSendsSinceDecision(request.Name) >= 2)
                {
                    await NeedsAttentionAsync($"\"{request.Name}\" was sent twice without a clear result. Check with the other side, then record whether it went through.", null);
                    return Next.Stop;
                }

                JToken? payload;
                var edited = target.Index == null ? _overrides[request.Name] : null;
                if (edited != null)
                {
                    // An admin wrote this body by hand ("send again with these values").
                    payload = edited.DeepClone();
                }
                else
                {
                    try
                    {
                        payload = FlowRunData.JsonSafe(new FlowMappingResolver(_data).Prepare(request.DataMapping, request.StaticData, request.MergeData, request.LocalTree)) ?? new JObject();
                    }
                    catch (Exception ex)
                    {
                        // The data is wrong (a required value is missing, an op failed): sending again
                        // would build the same body, so a person looks.
                        await AddStepAsync(target, KindCall, ServerFlowStepStatus.Failed, $"Could not build the request: {ex.Message}", null);
                        return await FailedRequestAsync(target, $"Could not build the request for {target.Label}: {ex.Message}", retryable: false);
                    }
                }

                await RenewLeaseAsync(target.StepName);
                var attempt = StepsOf(target.StepName).Count(s => s.Kind is KindCall or KindPoll) + 1;
                var step = await AddStepAsync(target, KindCall, ServerFlowStepStatus.Calling, null, null, attempt, payload);

                var watch = Stopwatch.StartNew();
                var result = await HoldingLeaseAsync(request.Kind == FlowRequestKind.Local
                    ? _owner._caller.RunLocalAsync(request.Operation, payload, _who)
                    : _owner._caller.CallDataSourceAsync(request.DataSourceId, payload, request.AuthToken, request.TimeoutSec, _who));
                watch.Stop();

                step.EndedOn = DateTimeOffset.UtcNow;
                step.DurationMs = (int)Math.Min(int.MaxValue, watch.ElapsedMilliseconds);
                step.Response = StepPayload.Store(result.Status == FlowCallStatus.Ok ? result.Response : result.ErrorBody, Hidden);
                // what the other side answered is masked in the error as it is in the kept answer
                var error = ErrorText(result.Error);

                // repeatConfig.continueOnError: a failed attempt answers { error, attempt } and the
                // polling goes on (a call that must never be sent twice cannot repeat at all).
                if (result.Status != FlowCallStatus.Ok && request.Repeat?.ContinueOnError == true)
                    return await RepeatAsync(target, step, new JObject { ["error"] = error, ["attempt"] = attempt }, new JObject());

                switch (result.Status)
                {
                    case FlowCallStatus.Ok:
                        return await AnsweredAsync(target, step, result.Response);

                    case FlowCallStatus.NotSent:
                        step.Status = ServerFlowStepStatus.Failed;
                        step.Error = StepPayload.Fit(error, ServerFlowRun.ErrorLength);
                        await _steps.SaveChangesAsync();
                        return await FailedRequestAsync(target, $"{target.Label} was not sent: {error}", retryable: false);

                    default: // Unclear
                        if (request.Class == FlowRequestClass.NonIdempotent)
                        {
                            step.Status = ServerFlowStepStatus.Unclear;
                            step.Error = StepPayload.Fit(error, ServerFlowRun.ErrorLength);
                            await _steps.SaveChangesAsync();

                            if (request.Guard == null)
                            {
                                await NeedsAttentionAsync(
                                    $"We do not know whether \"{request.Name}\" went through ({error}). Check with the other side, then record \"went through\" (with its values) or \"did not go through\".",
                                    error);
                                return Next.Stop;
                            }

                            // Give the other side time to finish, then ask.
                            await ReleaseAsync(ServerFlowRunStatus.Waiting, DateTimeOffset.UtcNow.AddSeconds(request.Guard.SettleSec),
                                $"Checking whether \"{request.Name}\" went through in {request.Guard.SettleSec} s.", error);
                            return Next.Stop;
                        }

                        step.Status = ServerFlowStepStatus.Failed;
                        step.Error = StepPayload.Fit(error, ServerFlowRun.ErrorLength);
                        await _steps.SaveChangesAsync();
                        return await FailedRequestAsync(target, $"{target.Label} failed: {error}", retryable: true);
                }
            }

            /// <summary>
            /// Waits for a call while keeping the lease: a call may take longer than the lease (a slow
            /// insurer, a long timeout), and a lease that ran out would let another worker take the run
            /// and send the call again.
            /// </summary>
            private async Task<T> HoldingLeaseAsync<T>(Task<T> call)
            {
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(10, _options.LeaseSeconds / 3)));
                while (true)
                {
                    // An unfinished tick ends when the timer is disposed: nothing is left waiting.
                    if (await Task.WhenAny(call, timer.WaitForNextTickAsync().AsTask()) == call)
                        return await call;
                    try
                    {
                        await RenewLeaseAsync(null);
                    }
                    catch (LeaseLostException)
                    {
                        // The call goes on in its own scope; keep its failure from going unobserved.
                        _ = call.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // A database hiccup: the next tick tries again, well before the lease runs out.
                        _owner._logger.LogWarning(ex, "Server flow run {RunId}: could not renew the lease during a call.", _run.Id);
                    }
                }
            }

            /// <summary>The call answered: check the body, keep the outputs (or ask again when it polls).</summary>
            private async Task<Next> AnsweredAsync(Target target, ServerFlowStep step, JToken? response)
            {
                var request = target.Request;
                string? failure;
                try
                {
                    failure = ResponseFailure(request, response);
                }
                catch (Exception ex)
                {
                    return await UnreadableAnswerAsync(target, step, $"a response check failed: {ex.Message}");
                }
                if (failure != null)
                {
                    if (request.Repeat?.ContinueOnError == true)
                        return await RepeatAsync(target, step, new JObject { ["error"] = failure, ["attempt"] = step.Attempt }, new JObject());
                    step.Status = ServerFlowStepStatus.Failed;
                    step.Error = StepPayload.Fit($"{RefusalPrefix}: {failure}", ServerFlowRun.ErrorLength);
                    await _steps.SaveChangesAsync();
                    // A business refusal is an answer: retrying sends the same thing and gets the same answer.
                    return await FailedRequestAsync(target, $"{target.Label} answered with an error: {failure}", retryable: false);
                }

                JToken? shaped;
                JObject outputs;
                try
                {
                    shaped = _data.Shape(request, response);
                    outputs = _data.ReadOutputs(request.Outputs, shaped);
                }
                catch (Exception ex)
                {
                    return await UnreadableAnswerAsync(target, step, ex.Message);
                }

                if (request.Repeat != null)
                    return await RepeatAsync(target, step, shaped, outputs);

                return await CompleteAsync(target, step, outputs);
            }

            /// <summary>
            /// The call answered but its values could not be read (a transform script failed, an op
            /// threw). For a call that must never be sent twice it DID go through, so a person records
            /// the values instead of the run sending it again; any other call fails and is not retried
            /// (it would read the same answer the same way).
            /// </summary>
            private async Task<Next> UnreadableAnswerAsync(Target target, ServerFlowStep step, string why)
            {
                var request = target.Request;
                if (request.Class == FlowRequestClass.NonIdempotent)
                {
                    step.Status = ServerFlowStepStatus.Unclear;
                    step.Error = StepPayload.Fit($"It went through, but its answer could not be read: {why}", ServerFlowRun.ErrorLength);
                    await _steps.SaveChangesAsync();
                    await NeedsAttentionAsync($"\"{request.Name}\" went through, but its answer could not be read ({why}). Record \"went through\" with its values; do not send it again.", why);
                    return Next.Stop;
                }

                step.Status = ServerFlowStepStatus.Failed;
                step.Error = StepPayload.Fit($"Could not read the outputs: {why}", ServerFlowRun.ErrorLength);
                await _steps.SaveChangesAsync();
                return await FailedRequestAsync(target, $"Could not read the outputs of {target.Label}: {why}", retryable: false);
            }

            /// <summary>The attempt is done: keep its outputs (a loop item keeps them on its own step only).</summary>
            private async Task<Next> CompleteAsync(Target target, ServerFlowStep step, JToken outputs)
            {
                step.Status = ServerFlowStepStatus.Succeeded;
                step.Outputs = outputs.ToString(Formatting.None);
                await _steps.SaveChangesAsync();
                if (target.Index != null)
                    return Next.Continue;

                await KeepOutputsAsync(target.Request, outputs, succeeded: true);
                await ForgetEditedBodyAsync(target.Request.Name);
                return await CheckPaymentAsync(target.Request);
            }

            /// <summary>
            /// repeatConfig (polling): the browser's runRepeatLoop. A "not done yet" attempt is kept as a
            /// "poll" step; the next one is sent after the strategy's delay, in this pickup when it is
            /// short (and this pickup has not waited long already), else the run waits and is picked up
            /// again when it is due.
            /// </summary>
            private async Task<Next> RepeatAsync(Target target, ServerFlowStep step, JToken? answer, JObject outputs)
            {
                var repeat = target.Request.Repeat!;
                var earlier = SinceDecision(target).Where(s => s.Kind == KindPoll && s.Id != step.Id).ToList();
                var attempt = earlier.Count + 1;

                bool done;
                try
                {
                    done = (repeat.StopIf != null && FlowConditions.Check(repeat.StopIf, answer, _data.Ops))
                        || Reached(repeat, answer)
                        || attempt >= repeat.MaxAttempts
                        || (repeat.MaxDurationMs is int max && earlier.Count > 0 && (DateTimeOffset.UtcNow - earlier[0].StartedOn).TotalMilliseconds > max);
                }
                catch (Exception ex)
                {
                    // The "done" / "stop" conditions could not be read (a script or an op failed):
                    // asking again would read the next answer the same way.
                    step.Status = ServerFlowStepStatus.Failed;
                    step.Error = StepPayload.Fit($"Could not check whether it is done: {ex.Message}", ServerFlowRun.ErrorLength);
                    await _steps.SaveChangesAsync();
                    await NeedsAttentionAsync($"Could not check whether {target.Label} is done: its condition failed ({ex.Message}).", ex.Message);
                    return Next.Stop;
                }
                if (done)
                {
                    JToken result = repeat.CollectAll
                        ? new JArray(earlier.Select(s => ParseToken(s.Outputs) ?? new JObject()).Append(outputs))
                        : outputs;
                    return await CompleteAsync(target, step, result);
                }

                var delayMs = NextDelayMs(repeat, attempt);
                step.Kind = KindPoll;
                step.Status = ServerFlowStepStatus.Failed;
                step.Outputs = outputs.ToString(Formatting.None);
                step.Error = StepPayload.Fit($"Not done yet (attempt {attempt} of {repeat.MaxAttempts}); asking again in {Seconds(delayMs)}.", ServerFlowRun.ErrorLength);
                await _steps.SaveChangesAsync();

                if (delayMs <= ShortWaitMs && _waitedMs + delayMs <= MaxPickupWaitMs)
                {
                    try
                    {
                        await Task.Delay(delayMs, _stopping);
                    }
                    catch (OperationCanceledException)
                    {
                        await ReleaseAsync(ServerFlowRunStatus.Waiting, DateTimeOffset.UtcNow, Stopping);
                        return Next.Stop;
                    }
                    _waitedMs += delayMs;
                    return await SendAsync(target);
                }

                await ReleaseAsync(ServerFlowRunStatus.Waiting, DateTimeOffset.UtcNow.AddMilliseconds(delayMs),
                    $"{target.Label} is not done yet (attempt {attempt} of {repeat.MaxAttempts}); asking again in {Seconds(delayMs)}.");
                return Next.Stop;
            }

            private static string Seconds(int ms) => ms < 1000 ? $"{ms} ms" : $"{Math.Round(ms / 1000.0, 1).ToString(CultureInfo.InvariantCulture)} s";

            /// <summary>Whether the answer is the one the repeat waits for (fixedCount never is).</summary>
            private bool Reached(FlowRepeat repeat, JToken? answer)
            {
                if (repeat.Strategy == "fixedCount")
                    return false;
                if (repeat.Until != null)
                    return FlowConditions.Check(repeat.Until, answer, _data.Ops);
                if (repeat.Strategy == "untilValue" && repeat.UntilPath != null)
                {
                    var actual = JsonPath.Get(answer, repeat.UntilPath);
                    var expected = repeat.UntilValue;
                    return repeat.UntilOperator switch
                    {
                        "===" => Js.StrictEquals(actual, expected),
                        "!==" => !Js.StrictEquals(actual, expected),
                        ">" => Js.GreaterThan(actual, expected),
                        "<" => Js.IsLess(actual, expected),
                        ">=" => Js.GreaterOrEqual(actual, expected),
                        "<=" => Js.LessOrEqual(actual, expected),
                        "exists" => !Js.IsNil(actual),
                        "notExists" => Js.IsNil(actual),
                        "includes" => actual is JArray list && list.Any(x => Js.StrictEquals(x, expected)),
                        _ => false,
                    };
                }
                return false;
            }

            /// <summary>The browser's delay after an attempt, per strategy (customDelayFn first).</summary>
            private int NextDelayMs(FlowRepeat repeat, int attempt)
            {
                switch (repeat.Strategy)
                {
                    case "polling":
                        return repeat.Exponential
                            ? (int)Math.Min(repeat.InitialDelayMs * Math.Pow(2, attempt), repeat.MaxDelayMs)
                            : repeat.InitialDelayMs;
                    case "exponentialBackoff":
                    {
                        var next = Math.Min(repeat.InitialDelayMs * Math.Pow(repeat.Multiplier, attempt), repeat.MaxDelayMs);
                        if (repeat.Jitter)
                            next += (Random.Shared.NextDouble() - 0.5) * 2 * next * 0.1;
                        return (int)Math.Max(0, next);
                    }
                }

                if (repeat.CustomDelayFn != null && _data.Ops.RunScript != null)
                {
                    try
                    {
                        var value = Js.ToNumber(_data.Ops.RunScript("ctx", repeat.CustomDelayFn, new JObject { ["attempt"] = attempt, ["previousDelay"] = repeat.DelayMs }));
                        if (!double.IsNaN(value))
                            return (int)Math.Clamp(value, 0, 3_600_000);
                    }
                    catch (FlowScriptException)
                    {
                        // as the browser: the configured delay
                    }
                }
                return repeat.DelayMs;
            }

            /// <summary>A body an admin wrote is used until its request succeeds, then it is dropped.</summary>
            private async Task ForgetEditedBodyAsync(string request)
            {
                if (_overrides[request] == null)
                    return;
                _overrides.Remove(request);
                var left = _overrides.HasValues ? _overrides.ToString(Formatting.None) : null;
                var now = DateTimeOffset.UtcNow;
                await UpdateRunAsync(s => s.SetProperty(r => r.RequestOverrides, left).SetProperty(r => r.UpdatedOn, now));
            }

            /// <summary>The guard: did the unclear nonIdempotent call go through?</summary>
            private async Task<Next> GuardAsync(FlowRequest request)
            {
                var guard = request.Guard;
                if (guard == null)
                {
                    await NeedsAttentionAsync(
                        $"We do not know whether \"{request.Name}\" went through. Check with the other side, then record \"went through\" (with its values) or \"did not go through\".",
                        null);
                    return Next.Stop;
                }

                var steps = StepsOf(request.Name);
                var lastCall = steps.LastOrDefault(s => s.Kind == KindCall);
                var settleUntil = (lastCall?.EndedOn ?? lastCall?.StartedOn ?? DateTimeOffset.UtcNow).AddSeconds(guard.SettleSec);
                if (settleUntil > DateTimeOffset.UtcNow)
                {
                    await ReleaseAsync(ServerFlowRunStatus.Waiting, settleUntil, $"Checking whether \"{request.Name}\" went through shortly.");
                    return Next.Stop;
                }

                var tries = steps.Count(s => s.Kind == KindGuard && s.Id > (lastCall?.Id ?? 0) && s.Status == ServerFlowStepStatus.Unclear);
                if (tries >= guard.MaxTries)
                {
                    await NeedsAttentionAsync($"Could not check whether \"{request.Name}\" went through ({tries} tries). Check with the other side, then record the result.", steps[^1].Error);
                    return Next.Stop;
                }

                JToken payload;
                try
                {
                    payload = FlowRunData.JsonSafe(new FlowMappingResolver(_data).Prepare(guard.DataMapping, null, false, false)) ?? new JObject();
                }
                catch (Exception ex)
                {
                    await AddStepAsync(request, KindGuard, ServerFlowStepStatus.Unclear, $"Could not build the check: {ex.Message}", null);
                    await NeedsAttentionAsync($"Could not build the check for \"{request.Name}\": {ex.Message}", null);
                    return Next.Stop;
                }

                await RenewLeaseAsync(request.Name);
                var step = await AddStepAsync(request, KindGuard, ServerFlowStepStatus.Calling, null, null, tries + 1, payload);
                var watch = Stopwatch.StartNew();
                var result = await HoldingLeaseAsync(_owner._caller.CallDataSourceAsync(guard.DataSourceId, payload, guard.AuthToken, 60, _who));
                step.EndedOn = DateTimeOffset.UtcNow;
                step.DurationMs = (int)Math.Min(int.MaxValue, watch.ElapsedMilliseconds);
                step.Response = StepPayload.Store(result.Status == FlowCallStatus.Ok ? result.Response : result.ErrorBody, Hidden);
                var error = ErrorText(result.Error);

                if (result.Status != FlowCallStatus.Ok)
                {
                    step.Status = ServerFlowStepStatus.Unclear;
                    step.Error = StepPayload.Fit(error, ServerFlowRun.ErrorLength);
                    await _steps.SaveChangesAsync();
                    if (tries + 1 >= guard.MaxTries || result.Status == FlowCallStatus.NotSent)
                    {
                        await NeedsAttentionAsync($"Could not check whether \"{request.Name}\" went through: {error}", error);
                        return Next.Stop;
                    }
                    await ReleaseAsync(ServerFlowRunStatus.Waiting, DateTimeOffset.UtcNow.AddSeconds(30 * (tries + 1)),
                        $"The check for \"{request.Name}\" failed; trying again.", error);
                    return Next.Stop;
                }

                bool done, notDone;
                try
                {
                    done = FlowConditions.Check(guard.DoneIf, result.Response, _data.Ops);
                    notDone = FlowConditions.Check(guard.NotDoneIf, result.Response, _data.Ops);
                }
                catch (Exception ex)
                {
                    step.Status = ServerFlowStepStatus.Unclear;
                    step.Error = StepPayload.Fit($"Its doneIf / notDoneIf could not be checked: {ex.Message}", ServerFlowRun.ErrorLength);
                    await _steps.SaveChangesAsync();
                    await NeedsAttentionAsync($"The check could not tell whether \"{request.Name}\" went through: its conditions failed ({ex.Message}). Check with the other side, then record the result.", ex.Message);
                    return Next.Stop;
                }

                if (done && !notDone)
                {
                    JObject outputs;
                    try
                    {
                        outputs = _data.ReadOutputs(guard.Outputs, result.Response);
                    }
                    catch (Exception ex)
                    {
                        step.Status = ServerFlowStepStatus.Unclear;
                        step.Error = StepPayload.Fit($"It went through, but its values could not be read: {ex.Message}", ServerFlowRun.ErrorLength);
                        await _steps.SaveChangesAsync();
                        await NeedsAttentionAsync($"\"{request.Name}\" went through, but its values could not be read from the check: {ex.Message}", null);
                        return Next.Stop;
                    }

                    step.Status = ServerFlowStepStatus.Succeeded;
                    step.Outputs = outputs.ToString(Formatting.None);
                    await _steps.SaveChangesAsync();
                    await KeepOutputsAsync(request, outputs, succeeded: true);
                    await ForgetEditedBodyAsync(request.Name);
                    return await CheckPaymentAsync(request);
                }

                if (notDone && !done)
                {
                    step.Status = ServerFlowStepStatus.Failed;
                    step.Error = "The check says it did not go through.";
                    await _steps.SaveChangesAsync();
                    return await SendAsync(Target.Of(request)); // at most one more (see UnclearSendsSinceDecision)
                }

                step.Status = ServerFlowStepStatus.Unclear;
                step.Error = done ? "The check matched both doneIf and notDoneIf." : "The check matched neither doneIf nor notDoneIf.";
                await _steps.SaveChangesAsync();
                await NeedsAttentionAsync($"The check could not tell whether \"{request.Name}\" went through ({step.Error}). Check with the other side, then record the result.", null);
                return Next.Stop;
            }

            /// <summary>A failed request (or loop item): retry it later, skip it (not critical), or stop for a person.</summary>
            private async Task<Next> FailedRequestAsync(Target target, string reason, bool retryable)
            {
                var request = target.Request;
                if (retryable && request.Class != FlowRequestClass.NonIdempotent)
                {
                    var failures = FailuresSinceDecision(target);
                    if (failures <= request.RetryMax)
                    {
                        var delay = request.RetryDelayFor(failures);
                        await ReleaseAsync(ServerFlowRunStatus.Waiting, DateTimeOffset.UtcNow.AddSeconds(delay),
                            $"{reason} Trying again in {delay} s (try {failures + 1} of {request.RetryMax + 1}).", reason);
                        return Next.Stop;
                    }
                }

                // A loop item without error settings of its own is caught by the browser ({ error,
                // item, index }) and the loop goes on; an optional request is given up.
                var itemGoesOn = target.Index != null && request.LoopItemFailuresContinue;
                if (!request.Critical || itemGoesOn)
                {
                    // Marks it as finished, so a later pickup does not send it again. What it answers
                    // instead (its fallback, else the browser's { error, name }) is kept.
                    var why = reason;
                    var instead = _data.GivenUpOutputs(request, target.Index, ref why);
                    await AddStepAsync(target, KindCall, ServerFlowStepStatus.Skipped, $"{GivenUp}: {why}", instead);
                    if (target.Index == null)
                        await KeepOutputsAsync(request, instead, succeeded: false);
                    return Next.Continue;
                }

                await NeedsAttentionAsync(reason, reason);
                return Next.Stop;
            }

            /// <summary>A call a stop interrupted (its step still says "Calling"): it counts as a failed attempt.</summary>
            private async Task InterruptedAsync(ServerFlowStep step)
            {
                step.Status = ServerFlowStepStatus.Failed;
                step.EndedOn = DateTimeOffset.UtcNow;
                step.Error = "Interrupted (the server stopped during the call).";
                await _steps.SaveChangesAsync();
            }

            #endregion

            #region Responses and outputs

            /// <summary>
            /// The first matching response check, described with what the other side said, read from
            /// the answer as it is kept (secrets and the flow's hidden values masked).
            /// </summary>
            private string? ResponseFailure(FlowRequest request, JToken? response)
            {
                foreach (var check in request.ResponseChecks)
                {
                    if (!FlowConditions.Check(check, response, _data.Ops))
                        continue;
                    var words = FlowErrorMessages.AnswerWords(StepPayload.Hide(response, Hidden), request.ResponseMessagePath);
                    return words == null
                        ? "a response check matched (the answer has no message; see the answer below)"
                        : StepPayload.Fit(words, ServerFlowRun.ReasonLength);
                }
                return null;
            }

            /// <summary>A request's result is final: keep it, and with it the run's outputs, reference and last error.</summary>
            private async Task KeepOutputsAsync(FlowRequest request, JToken outputs, bool succeeded)
            {
                _data.Store(request, outputs);
                await SaveProgressAsync(succeeded);
            }

            /// <summary>
            /// Writes what the run knows to its row with one update, only when something changed: the
            /// outputs, the customer-facing reference (kept as soon as it exists, so a later step that
            /// stops for a person does not take it from the customer), and the last error, cleared once
            /// a request succeeds after it.
            /// </summary>
            private async Task SaveProgressAsync(bool succeeded = false)
            {
                var outputs = FlowRunData.OutputsOnly(_data.Results, _definition!.Requests.Select(r => r.Name)).ToString(Formatting.None);
                var reference = ReferenceNow() ?? _reference;
                var lastError = succeeded ? null : _lastError;
                if (outputs == _savedOutputs && reference == _reference && lastError == _lastError)
                    return;

                var now = DateTimeOffset.UtcNow;
                await UpdateRunAsync(s => s
                    .SetProperty(r => r.Outputs, outputs)
                    .SetProperty(r => r.Reference, reference)
                    .SetProperty(r => r.LastError, lastError)
                    .SetProperty(r => r.UpdatedOn, now));
                (_savedOutputs, _reference, _lastError) = (outputs, reference, lastError);
            }

            /// <summary>The run's reference as the results hold it now (null until its request has answered).</summary>
            private string? ReferenceNow()
            {
                var value = _definition!.Reference is { } from ? _data.OutputOf(from) : null;
                return Js.IsNil(value) ? null : StepPayload.Fit(Js.ToJsString(value), ServerFlowRun.ReferenceLength);
            }

            /// <summary>settings.paymentCheck: right after the request that gives the expected amount.</summary>
            private async Task<Next> CheckPaymentAsync(FlowRequest request)
            {
                var check = _definition!.Settings.PaymentAmountFrom;
                if (check == null || check.Request != request.Name)
                    return Next.Continue;

                var expected = Js.ToNumber(_data.OutputOf(check));
                var paid = _definition!.Settings.PaymentCompareTo == "orderTotal"
                    ? Js.ToNumber(_data.Order?["total"])
                    : Js.ToNumber(_data.Order?["payment"]?["amount"]);

                // In decimal, rounded to the halala: a double difference of 0.01 can come out 0.010000001.
                static decimal? Money(double v) => double.IsNaN(v) || double.IsInfinity(v) || Math.Abs(v) > 1e15 ? null : Math.Round((decimal)v, 2);
                var expectedMoney = Money(expected);
                var paidMoney = Money(paid);
                if (expectedMoney == null || paidMoney == null || Math.Abs(expectedMoney.Value - paidMoney.Value) > _definition!.Settings.PaymentTolerance)
                {
                    await NeedsAttentionAsync(
                        $"Amount check failed: \"{check}\" is {Show(expected)} but the {(_definition!.Settings.PaymentCompareTo == "orderTotal" ? "order total" : "paid amount")} is {Show(paid)}. Nothing after \"{request.Name}\" was sent.",
                        null);
                    return Next.Stop;
                }

                return Next.Continue;
            }

            #endregion

            #region Run state

            private async Task SucceedAsync()
            {
                // The final outputs and reference, whatever an earlier write left out.
                var outputs = FlowRunData.OutputsOnly(_data.Results, _definition!.Requests.Select(r => r.Name)).ToString(Formatting.None);
                var reference = ReferenceNow() ?? _reference;
                var now = DateTimeOffset.UtcNow;
                await UpdateRunAsync(s => s
                    .SetProperty(r => r.Status, ServerFlowRunStatus.Succeeded)
                    .SetProperty(r => r.Outputs, outputs)
                    .SetProperty(r => r.Reference, reference)
                    .SetProperty(r => r.Reason, (string?)null)
                    .SetProperty(r => r.LastError, (string?)null)
                    .SetProperty(r => r.CurrentStep, (string?)null)
                    .SetProperty(r => r.NextAttemptAt, (DateTimeOffset?)null)
                    .SetProperty(r => r.LeaseOwner, (string?)null)
                    .SetProperty(r => r.LeaseUntil, (DateTimeOffset?)null)
                    .SetProperty(r => r.CompletedOn, now)
                    .SetProperty(r => r.UpdatedOn, now));

                await _log.AddInfo("ServerFlows", $"Run {_run.Id} ({_run.FlowName}) for {_run.SubjectType} {_run.SubjectId} succeeded{(reference == null ? "" : $": {reference}")}.", null, _run.SiteId);
            }

            /// <summary>
            /// The run stops until a person looks. <paramref name="reason"/> and <paramref name="error"/>
            /// are already safe to show (built from masked answers); they go to the run, the log and the
            /// staff e-mail.
            /// </summary>
            private async Task NeedsAttentionAsync(string reason, string? error)
            {
                var now = DateTimeOffset.UtcNow;
                var why = StepPayload.Fit(reason, ServerFlowRun.ReasonLength);
                var lastError = StepPayload.Fit(error, ServerFlowRun.ErrorLength);
                await UpdateRunAsync(s => s
                    .SetProperty(r => r.Status, ServerFlowRunStatus.NeedsAttention)
                    .SetProperty(r => r.Reason, why)
                    .SetProperty(r => r.LastError, lastError)
                    .SetProperty(r => r.NextAttemptAt, (DateTimeOffset?)null)
                    .SetProperty(r => r.LeaseOwner, (string?)null)
                    .SetProperty(r => r.LeaseUntil, (DateTimeOffset?)null)
                    .SetProperty(r => r.UpdatedOn, now));
                _lastError = lastError;

                await _log.AddError("ServerFlows", $"run {_run.Id}", $"Run {_run.Id} ({_run.FlowName}) for {_run.SubjectType} {_run.SubjectId} needs attention: {why}", null, _run.SiteId);
                await _owner._alerts.NeedsAttentionAsync(_run, why!, _definition?.Settings.AttentionEmails ?? Array.Empty<string>(), _definition?.Settings.AttentionTemplateId);
            }

            /// <summary>
            /// Lets the run go until <paramref name="nextAttemptAt"/>. A planned wait (a retry, polling, a
            /// guard's settle time, a stop) gives back this pickup's count, so only pickups that ended
            /// unexpectedly count towards MaxRunPickups. A wait shorter than the sweep is handed to the
            /// worker again when it is due.
            /// </summary>
            private async Task ReleaseAsync(ServerFlowRunStatus status, DateTimeOffset nextAttemptAt, string reason, string? error = null, bool unplanned = false)
            {
                var why = StepPayload.Fit(reason, ServerFlowRun.ReasonLength);
                var lastError = error == null ? _lastError : StepPayload.Fit(error, ServerFlowRun.ErrorLength);
                var giveBack = unplanned ? 0 : 1;
                var now = DateTimeOffset.UtcNow;
                await UpdateRunAsync(s => s
                    .SetProperty(r => r.Status, status)
                    .SetProperty(r => r.NextAttemptAt, nextAttemptAt)
                    .SetProperty(r => r.Reason, why)
                    .SetProperty(r => r.LastError, lastError)
                    .SetProperty(r => r.LeaseOwner, (string?)null)
                    .SetProperty(r => r.LeaseUntil, (DateTimeOffset?)null)
                    .SetProperty(r => r.Attempts, r => r.Attempts > giveBack ? r.Attempts - giveBack : 0)
                    .SetProperty(r => r.UpdatedOn, now));
                _lastError = lastError;

                if (status == ServerFlowRunStatus.Waiting)
                    _owner._queue.EnqueueAt(_run.Id, nextAttemptAt, TimeSpan.FromSeconds(Math.Clamp(_options.SweepSeconds, 5, 600)));
            }

            public async Task FailUnexpectedlyAsync(Exception ex)
            {
                try
                {
                    var error = ErrorText(ex.GetBaseException().Message);
                    if (_run.Attempts >= _options.MaxRunPickups)
                        await NeedsAttentionAsync("The run stopped on an unexpected error several times.", error);
                    else
                        await ReleaseAsync(ServerFlowRunStatus.Waiting, DateTimeOffset.UtcNow.AddSeconds(Math.Min(3600, 30 * _run.Attempts)),
                            "Stopped on an unexpected error; trying again.", error, unplanned: true);
                }
                catch (Exception inner)
                {
                    // The lease expires on its own and the sweep resumes the run.
                    _owner._logger.LogError(inner, "Server flow run {RunId}: could not record the failure.", _run.Id);
                }
            }

            /// <summary>
            /// An error text as the run uses it: the other side's answer in it masked like the kept
            /// answer, and cut to a sentence's length. It goes into the run's reason, the log, the
            /// e-mail, and into outputs ({ error, attempt } of a polling attempt, a given-up call's
            /// { error, name }); the whole answer stays only in the step's kept answer.
            /// </summary>
            private string? ErrorText(string? error) => StepPayload.Fit(StepPayload.SafeError(error, Hidden), MaxErrorText);

            private const int MaxErrorText = 1000;

            /// <summary>Extends the lease (and says which step the run is on, when given).</summary>
            private Task RenewLeaseAsync(string? currentStep)
            {
                var now = DateTimeOffset.UtcNow;
                var until = now.AddSeconds(_options.LeaseSeconds);
                if (currentStep == null)
                    return UpdateRunAsync(s => s.SetProperty(r => r.LeaseUntil, until).SetProperty(r => r.UpdatedOn, now));
                var step = StepPayload.Fit(currentStep, ServerFlowRun.NameLength);
                return UpdateRunAsync(s => s
                    .SetProperty(r => r.LeaseUntil, until)
                    .SetProperty(r => r.CurrentStep, step)
                    .SetProperty(r => r.UpdatedOn, now));
            }

            /// <summary>
            /// Every write to the run checks the lease is still ours and the run still running
            /// (fencing): an admin's stop, or another worker's claim, wins over a worker that lost track.
            /// </summary>
            private async Task UpdateRunAsync(Expression<Func<SetPropertyCalls<ServerFlowRun>, SetPropertyCalls<ServerFlowRun>>> set)
            {
                var id = _run.Id;
                var me = _workerId;
                var changed = await _runs.Query()
                    .Where(r => r.Id == id && r.LeaseOwner == me && r.Status == ServerFlowRunStatus.Running)
                    .ExecuteUpdateAsync(set, CancellationToken.None);
                if (changed == 0)
                    throw new LeaseLostException();
            }

            /// <summary>
            /// A flow's own condition (a group's or a request's). One whose script or value ops fail
            /// stops the run for a person: guessing either way could send what must not be sent.
            /// Null when it stopped the run.
            /// </summary>
            private async Task<bool?> ConditionAsync(JObject? list, string what)
            {
                try
                {
                    return FlowConditions.Check(list, _data.ConditionData(), _data.Ops);
                }
                catch (Exception ex)
                {
                    await NeedsAttentionAsync($"Could not check {what}: {ex.Message}", ex.Message);
                    return null;
                }
            }

            #endregion

            #region Steps

            private Task<ServerFlowStep> AddStepAsync(FlowRequest request, string kind, ServerFlowStepStatus status, string? error, JToken? outputs, int attempt = 1, JToken? sent = null) =>
                AddStepAsync(Target.Of(request), kind, status, error, outputs, attempt, sent);

            private async Task<ServerFlowStep> AddStepAsync(Target target, string kind, ServerFlowStepStatus status, string? error, JToken? outputs, int attempt = 1, JToken? sent = null)
            {
                var request = target.Request;
                var step = new ServerFlowStep
                {
                    RunId = _run.Id,
                    GroupName = StepPayload.Fit(request.GroupName, ServerFlowRun.NameLength),
                    RequestName = StepPayload.Fit(target.StepName, ServerFlowRun.NameLength),
                    Kind = kind,
                    Attempt = attempt,
                    Status = status,
                    Error = StepPayload.Fit(error, ServerFlowRun.ErrorLength),
                    Outputs = outputs?.ToString(Formatting.None),
                    Request = StepPayload.Store(sent, Hidden),
                };
                if (status != ServerFlowStepStatus.Calling)
                    step.EndedOn = step.StartedOn;

                _steps.Add(step);
                await _steps.SaveChangesAsync();
                Index(step);
                return step;
            }

            /// <summary>Files a step under its name, and its request ("name[3]" is a step of "name") as started.</summary>
            private void Index(ServerFlowStep step)
            {
                if (!_stepsByName.TryGetValue(step.RequestName, out var list))
                    _stepsByName[step.RequestName] = list = new List<ServerFlowStep>();
                list.Add(step);
                var bracket = step.RequestName.IndexOf('[');
                _started.Add(bracket > 0 ? step.RequestName[..bracket] : step.RequestName);
            }

            /// <summary>The steps of one step name, oldest first.</summary>
            private IReadOnlyList<ServerFlowStep> StepsOf(string name) =>
                _stepsByName.TryGetValue(name, out var list) ? list : Array.Empty<ServerFlowStep>();

            /// <summary>A group whose condition was false: it has no result to reshape.</summary>
            private bool GroupSkipped(FlowGroup group) =>
                group.Requests.All(r => StepsOf(r.Name).LastOrDefault() is { Status: ServerFlowStepStatus.Skipped, Error: GroupConditionFalse });

            /// <summary>The group's transformResult, once its requests are done.</summary>
            private async Task<Next> TransformGroupAsync(FlowGroup group)
            {
                if (group.Transform == null)
                    return Next.Continue;
                try
                {
                    _data.ApplyGroupTransform(group);
                    return Next.Continue;
                }
                catch (Exception ex)
                {
                    await NeedsAttentionAsync($"The result of group \"{group.Name}\" could not be reshaped (transformResult): {ex.Message}", ex.Message);
                    return Next.Stop;
                }
            }

            private bool IsSettled(string request)
            {
                var last = StepsOf(request).LastOrDefault();
                return last != null && last.Status is ServerFlowStepStatus.Succeeded or ServerFlowStepStatus.Skipped;
            }

            /// <summary>Steps after the last decision of a person (which restarts the counting).</summary>
            private IEnumerable<ServerFlowStep> SinceDecision(string request) => SinceDecision(new Target(null!, request, null));

            /// <summary>
            /// A target's steps after the last decision of a person. A loop item also counts the
            /// decisions taken on its request (Try again is recorded there).
            /// </summary>
            private IEnumerable<ServerFlowStep> SinceDecision(Target target)
            {
                var steps = StepsOf(target.StepName);
                var manual = steps.Where(s => s.Kind == KindManual);
                if (target.Index != null)
                    manual = manual.Concat(StepsOf(target.Request.Name).Where(s => s.Kind == KindManual));
                var lastManualId = manual.Select(s => s.Id).DefaultIfEmpty(0).Max();
                return steps.Where(s => s.Id > lastManualId);
            }

            private int UnclearSendsSinceDecision(string request) =>
                SinceDecision(request).Count(s => s.Kind == KindCall && s.Status is ServerFlowStepStatus.Unclear or ServerFlowStepStatus.Calling);

            private int FailuresSinceDecision(Target target) =>
                SinceDecision(target).Count(s => s.Kind == KindCall && s.Status == ServerFlowStepStatus.Failed);

            #endregion

            private static FlowIdentity IdentityOf(JObject context)
            {
                // A run a page started calls as that visitor; an order's run as the order's customer.
                if (context["caller"] is JObject caller)
                    return new FlowIdentity(
                        SiteId: (long?)caller["siteId"] ?? 0,
                        UserId: (long?)caller["userId"] ?? 0,
                        CultureId: (long?)caller["cultureId"] ?? 0,
                        CartId: (long?)caller["cartId"]);
                var order = context["order"];
                return new FlowIdentity(
                    SiteId: (long?)order?["siteId"] ?? 0,
                    UserId: (long?)order?["customer"]?["id"] ?? 0,
                    CultureId: (long?)order?["cultureId"] ?? 0,
                    CartId: (long?)order?["cartId"]);
            }

            /// <summary>A step's outputs: an object, or a list for a loop / collected polling answers.</summary>
            private static JToken? ParseToken(string? json)
            {
                if (string.IsNullOrWhiteSpace(json))
                    return null;
                var token = JsonConvert.DeserializeObject<JToken>(json, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
                return token is JObject or JArray ? token : null;
            }

            private static JObject ParseObject(string? json)
            {
                if (string.IsNullOrWhiteSpace(json))
                    return new JObject();
                return JsonConvert.DeserializeObject<JToken>(json, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None }) as JObject ?? new JObject();
            }

            private static string Show(double value) => double.IsNaN(value) ? "missing" : value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
