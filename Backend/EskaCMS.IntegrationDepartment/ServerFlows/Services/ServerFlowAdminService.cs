using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.ServerFlows;
using EskaCMS.Infrastructure.Data;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.IntegrationDepartment.ServerFlows.Engine;
using EskaCMS.IntegrationDepartment.ServerFlows.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>The answer of an admin action: a value, or why it was refused (with the definition's issues when there are some).</summary>
    public sealed class AdminResult<T>
    {
        public T? Value { get; init; }
        public string? Error { get; init; }
        public bool NotFound { get; init; }
        public IReadOnlyList<FlowIssue> Issues { get; init; } = Array.Empty<FlowIssue>();
        public bool Ok => Error == null && !NotFound;

        public static AdminResult<T> Success(T value, IReadOnlyList<FlowIssue>? issues = null) => new() { Value = value, Issues = issues ?? Array.Empty<FlowIssue>() };
        public static AdminResult<T> Fail(string error, IReadOnlyList<FlowIssue>? issues = null) => new() { Error = error, Issues = issues ?? Array.Empty<FlowIssue>() };
        public static AdminResult<T> Missing(string what) => new() { NotFound = true, Error = $"{what} not found." };
    }

    public sealed record FlowVersionInfo(long Id, string Name, int Version, string Status, string Trigger, string? Note,
        DateTimeOffset CreatedOn, long CreatedById, DateTimeOffset? ActivatedOn, long? ActivatedById, string Checksum,
        string? CreatedByName = null, string? ActivatedByName = null);

    public sealed record FlowDetails(FlowVersionInfo Info, JToken? Definition, IReadOnlyList<FlowIssue> Issues);

    public sealed record RunSummary(long Id, string FlowName, long FlowId, string SubjectType, long SubjectId, string Status,
        string? CurrentStep, string? Reference, string? Reason, int Attempts, DateTimeOffset? NextAttemptAt,
        DateTimeOffset CreatedOn, DateTimeOffset UpdatedOn, DateTimeOffset? CompletedOn);

    public sealed record StepInfo(long Id, string Group, string Request, string Kind, int Attempt, string Status,
        DateTimeOffset StartedOn, DateTimeOffset? EndedOn, int? DurationMs, string? Error, JToken? Outputs, long? DecidedById,
        bool HasSent = false, bool HasAnswer = false, string? DecidedByName = null);

    /// <summary>What one attempt sent and what it got back.</summary>
    public sealed record StepExchange(long StepId, string Request, string Kind, int Attempt, string Status, DateTimeOffset StartedOn,
        int? DurationMs, string? Error, JToken? Sent, JToken? Answer);

    /// <summary>
    /// The body a request would send next, for the "send again" editor: the admin's edited body if
    /// there is one, else the body of its last attempt, else the one the flow builds now.
    /// </summary>
    public sealed record NextBody(string Request, string Class, string Source, JToken? Body, string? BuildError, bool CanSend, string? Why);

    /// <summary>One request of the flow as the admin page shows it: where the run is.</summary>
    /// <summary>CanSendAgain: an optional request that failed for good; it can be sent again on its own.</summary>
    public sealed record RequestProgress(string Group, string Request, string Class, bool Critical, string State,
        IReadOnlyList<string> Outputs, bool CanDecide, bool CanSendAgain = false);

    public sealed record RunDetails(RunSummary Run, string? LastError, IReadOnlyList<RequestProgress> Requests, IReadOnlyList<StepInfo> Steps, JToken? Context, JToken? Outputs);

    /// <summary>
    /// A flow a page can start or read, as the request editor shows it. Active false: only a draft so
    /// far. Trigger "orderPaid": a payment's flow, which a payment page only reads ("Read the result"
    /// with the order's id).
    /// </summary>
    public sealed record PageFlowInfo(string Name, bool Active, string Who, string Once, string? OnceInput, string AfterFailure, int WaitSec,
        IReadOnlyList<PageFlowInput> Inputs, IReadOnlyList<string> Results, string Trigger = FlowSettings.PageTrigger);

    /// <summary>
    /// What the runs list is narrowed to: statuses (none = all), a search (a number: the run's, or
    /// the order, customer or cart it is for; other text: part of its reference), a flow, and when it
    /// started (From inclusive, To exclusive).
    /// </summary>
    public sealed record RunFilter(IReadOnlyList<ServerFlowRunStatus> Statuses, string? Search, string? FlowName, DateTimeOffset? From, DateTimeOffset? To);

    public sealed record PageFlowInput(string Name, bool Required, string Type, int? MaxLength);

    /// <summary>
    /// What the admin API does. Every method takes the site the admin works on; nothing of
    /// another site can be read or changed.
    /// </summary>
    public sealed class ServerFlowAdminService
    {
        private readonly IRepository<ServerFlow> _flows;
        private readonly IRepository<ServerFlowRun> _runs;
        private readonly IRepository<ServerFlowStep> _steps;
        private readonly IRepository<ApiMapper> _dataSources;
        private readonly ServerFlowCatalog _catalog;
        private readonly ServerFlowQueue _queue;
        private readonly ServerFlowSignals _signals;
        private readonly IOptionsMonitor<ServerFlowOptions> _options;
        private readonly IConfiguration _configuration;
        private readonly IRepository<User> _users;
        private readonly IRepositoryWithTypedId<EmailTemplates, string> _templates;

        public ServerFlowAdminService(
            IRepository<ServerFlow> flows,
            IRepository<ServerFlowRun> runs,
            IRepository<ServerFlowStep> steps,
            IRepository<ApiMapper> dataSources,
            ServerFlowCatalog catalog,
            ServerFlowQueue queue,
            ServerFlowSignals signals,
            IOptionsMonitor<ServerFlowOptions> options,
            IConfiguration configuration,
            IRepository<User> users,
            IRepositoryWithTypedId<EmailTemplates, string> templates)
        {
            _flows = flows;
            _runs = runs;
            _steps = steps;
            _dataSources = dataSources;
            _catalog = catalog;
            _queue = queue;
            _signals = signals;
            _options = options;
            _configuration = configuration;
            _users = users;
            _templates = templates;
        }

        #region Flows

        public async Task<IReadOnlyList<FlowVersionInfo>> ListFlowsAsync(long siteId)
        {
            var rows = await _flows.Query().AsNoTracking()
                .Where(f => f.SiteId == siteId)
                .OrderBy(f => f.Name).ThenByDescending(f => f.Version)
                .ToListAsync();
            var names = await NamesAsync(rows.Select(f => (long?)f.CreatedById).Concat(rows.Select(f => f.ActivatedById)));
            return rows.Select(f => Named(Info(f), names)).ToList();
        }

        public async Task<AdminResult<FlowDetails>> GetFlowAsync(long siteId, long id)
        {
            var flow = await _flows.Query().AsNoTracking().FirstOrDefaultAsync(f => f.Id == id && f.SiteId == siteId);
            if (flow == null)
                return AdminResult<FlowDetails>.Missing("Flow version");

            var parsed = FlowDefinitionParser.Parse(flow.Definition, _options.CurrentValue.TimeZoneId, int.MaxValue);
            var definition = Try(() => JsonConvert.DeserializeObject<JToken>(flow.Definition, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None }));
            return AdminResult<FlowDetails>.Success(new FlowDetails(await NamedAsync(Info(flow)), definition, parsed.Issues));
        }

        /// <summary>The users' names by id ("user 7" when a user has none or is gone).</summary>
        private async Task<Dictionary<long, string>> NamesAsync(IEnumerable<long?> ids)
        {
            var wanted = ids.Where(i => i.HasValue && i.Value > 0).Select(i => i!.Value).Distinct().ToList();
            if (wanted.Count == 0)
                return new Dictionary<long, string>();
            var users = await _users.Query().AsNoTracking()
                .Where(u => wanted.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.UserName, u.Email })
                .ToListAsync();
            return users.ToDictionary(u => u.Id, u => DisplayName(u.Id, u.FullName, u.UserName, u.Email));
        }

        private static string DisplayName(long id, string? fullName, string? userName, string? email) =>
            !string.IsNullOrWhiteSpace(fullName) ? fullName.Trim()
            : !string.IsNullOrWhiteSpace(userName) ? userName.Trim()
            : !string.IsNullOrWhiteSpace(email) ? email.Trim()
            : $"user {id}";

        private static string? NameOf(IReadOnlyDictionary<long, string> names, long? id) =>
            id is long v && v > 0 ? (names.TryGetValue(v, out var n) ? n : $"user {v}") : null;

        private static FlowVersionInfo Named(FlowVersionInfo info, IReadOnlyDictionary<long, string> names) =>
            info with { CreatedByName = NameOf(names, info.CreatedById), ActivatedByName = NameOf(names, info.ActivatedById) };

        private async Task<FlowVersionInfo> NamedAsync(FlowVersionInfo info) =>
            Named(info, await NamesAsync(new long?[] { info.CreatedById, info.ActivatedById }));

        /// <summary>Checks a definition without saving it: the parser's issues plus what only this server knows.</summary>
        public async Task<IReadOnlyList<FlowIssue>> ValidateAsync(long siteId, string definition)
        {
            var parsed = Parse(definition);
            var issues = parsed.Issues.ToList();
            if (parsed.Definition != null)
                issues.AddRange(await ServerIssuesAsync(siteId, parsed.Definition));
            return issues;
        }

        /// <summary>
        /// Saves a new version of a flow and switches it on at once: new runs use it from now on and
        /// the version that was on stops starting runs (runs already started keep the version they
        /// started with). Only a version that passes the checks is saved.
        /// </summary>
        public async Task<AdminResult<FlowVersionInfo>> SaveAsync(long siteId, long userId, string name, string? note, string definition)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0 || name.Length > 100 || !name.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
                return AdminResult<FlowVersionInfo>.Fail("Name the flow with 1-100 letters, digits, - or _ (e.g. \"pa-after-payment\").");

            var parsed = Parse(definition);
            var issues = parsed.Issues.ToList();
            if (parsed.Definition == null)
                return AdminResult<FlowVersionInfo>.Fail("The flow has errors; nothing was saved.", issues);
            issues.AddRange(await ServerIssuesAsync(siteId, parsed.Definition));
            if (issues.Any(i => i.Severity == FlowIssueSeverity.Error))
                return AdminResult<FlowVersionInfo>.Fail("The flow has errors; nothing was saved.", issues);

            var version =await _flows.Query().Where(f => f.SiteId == siteId && f.Name == name).Select(f => (int?)f.Version).MaxAsync() ?? 0;
            var flow = new ServerFlow
            {
                SiteId = siteId,
                Name = name,
                Version = version + 1,
                Status = ServerFlowStatus.Draft,
                Trigger = parsed.Definition.Settings.Trigger,
                Definition = definition,
                Checksum = Checksum(definition),
                Note = StepPayload.Fit(note, 500),
                CreatedById = userId,
            };
            _flows.Add(flow);
            try
            {
                await _flows.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return AdminResult<FlowVersionInfo>.Fail("Another version of this flow was saved at the same moment; reload and save again.");
            }

            await SwitchOnAsync(siteId, userId, flow);
            return AdminResult<FlowVersionInfo>.Success(await NamedAsync(Info(flow)), issues);
        }

        /// <summary>
        /// Switches on a version that is not on: a draft saved before saving switched versions on, or
        /// an older version to go back to. The version that was on stops starting runs. The admin
        /// sends the checksum of the version they looked at, so exactly that text goes live.
        /// </summary>
        public async Task<AdminResult<FlowVersionInfo>> ActivateAsync(long siteId, long userId, long id, string? checksum)
        {
            var flow = await _flows.Query().FirstOrDefaultAsync(f => f.Id == id && f.SiteId == siteId);
            if (flow == null)
                return AdminResult<FlowVersionInfo>.Missing("Flow version");
            if (flow.Status == ServerFlowStatus.Active)
                return AdminResult<FlowVersionInfo>.Fail("This version is already on.");
            if (!string.Equals(checksum, flow.Checksum, StringComparison.OrdinalIgnoreCase))
                return AdminResult<FlowVersionInfo>.Fail("The checksum does not match this version: reload it, look at it again, then switch it on.");

            var parsed = Parse(flow.Definition);
            if (parsed.Definition == null)
                return AdminResult<FlowVersionInfo>.Fail("This version does not pass the checks any more; save a corrected version.", parsed.Issues);
            var issues = parsed.Issues.ToList();
            issues.AddRange(await ServerIssuesAsync(siteId, parsed.Definition));
            if (issues.Any(i => i.Severity == FlowIssueSeverity.Error))
                return AdminResult<FlowVersionInfo>.Fail("This version does not pass the checks any more; save a corrected version.", issues);

            await SwitchOnAsync(siteId, userId, flow);
            return AdminResult<FlowVersionInfo>.Success(await NamedAsync(Info(flow)), issues);
        }

        /// <summary>Makes a tracked version the active one of its flow; the version that was on is archived.</summary>
        private async Task SwitchOnAsync(long siteId, long userId, ServerFlow flow)
        {
            var now = DateTimeOffset.UtcNow;
            await _flows.ExecuteInTransactionAsync(async () =>
            {
                var current = await _flows.Query()
                    .Where(f => f.SiteId == siteId && f.Name == flow.Name && f.Status == ServerFlowStatus.Active && f.Id != flow.Id)
                    .ToListAsync();
                foreach (var old in current)
                {
                    old.Status = ServerFlowStatus.Archived;
                    old.ArchivedOn = now;
                    old.ArchivedById = userId;
                }
                flow.Status = ServerFlowStatus.Active;
                flow.ActivatedOn = now;
                flow.ActivatedById = userId;
                flow.ArchivedOn = null;
                flow.ArchivedById = null;
                await _flows.SaveChangesAsync();
            });
            _catalog.Invalidate(siteId);
        }

        /// <summary>Stops a version from starting new runs. Runs already created keep the version they started with.</summary>
        public async Task<AdminResult<FlowVersionInfo>> ArchiveAsync(long siteId, long userId, long id)
        {
            var flow = await _flows.Query().FirstOrDefaultAsync(f => f.Id == id && f.SiteId == siteId);
            if (flow == null)
                return AdminResult<FlowVersionInfo>.Missing("Flow version");
            if (flow.Status == ServerFlowStatus.Archived)
                return AdminResult<FlowVersionInfo>.Success(await NamedAsync(Info(flow)));

            flow.Status = ServerFlowStatus.Archived;
            flow.ArchivedOn = DateTimeOffset.UtcNow;
            flow.ArchivedById = userId;
            await _flows.SaveChangesAsync();
            _catalog.Invalidate(siteId);
            return AdminResult<FlowVersionInfo>.Success(await NamedAsync(Info(flow)));
        }

        #endregion

        #region Runs

        /// <summary>One page of the site's runs (newest first) that match the filter, and how many match.</summary>
        public async Task<(IReadOnlyList<RunSummary> Items, int Total)> ListRunsAsync(long siteId, RunFilter filter, int page, int pageSize)
        {
            var query = Filtered(siteId, filter);
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);
            var total = await query.CountAsync();
            // Only what the list shows (the page asks every few seconds): never the run's data, outputs or edited bodies.
            var rows = await query.OrderByDescending(r => r.Id).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(r => new ServerFlowRun
                {
                    Id = r.Id, FlowName = r.FlowName, FlowId = r.FlowId, SubjectType = r.SubjectType, SubjectId = r.SubjectId, Status = r.Status,
                    CurrentStep = r.CurrentStep, Reference = r.Reference, Reason = r.Reason, Attempts = r.Attempts, NextAttemptAt = r.NextAttemptAt,
                    CreatedOn = r.CreatedOn, UpdatedOn = r.UpdatedOn, CompletedOn = r.CompletedOn,
                })
                .ToListAsync();
            return (rows.Select(Summary).ToList(), total);
        }

        /// <summary>How many of the site's runs match the filter (its statuses aside), per status.</summary>
        public async Task<IReadOnlyDictionary<string, int>> CountRunsAsync(long siteId, RunFilter filter)
        {
            var counts = await Filtered(siteId, filter with { Statuses = Array.Empty<ServerFlowRunStatus>() })
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            return Enum.GetValues<ServerFlowRunStatus>().ToDictionary(s => s.ToString(), s => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0);
        }

        private IQueryable<ServerFlowRun> Filtered(long siteId, RunFilter filter)
        {
            var query = _runs.Query().AsNoTracking().Where(r => r.SiteId == siteId);
            if (filter.Statuses.Count > 0)
            {
                var statuses = filter.Statuses.ToList();
                query = query.Where(r => statuses.Contains(r.Status));
            }
            if (!string.IsNullOrWhiteSpace(filter.FlowName))
            {
                var name = filter.FlowName.Trim();
                query = query.Where(r => r.FlowName == name);
            }
            if (filter.From is { } from)
                query = query.Where(r => r.CreatedOn >= from);
            if (filter.To is { } to)
                query = query.Where(r => r.CreatedOn < to);

            var search = filter.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                // A number is the run's own, or the order, customer or cart it is for; other text is part of its reference.
                if (long.TryParse(search, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                    query = query.Where(r => r.Id == number || r.SubjectId == number);
                else
                    query = query.Where(r => r.Reference != null && r.Reference.Contains(search));
            }
            return query;
        }

        public async Task<AdminResult<RunDetails>> GetRunAsync(long siteId, long runId)
        {
            var run = await _runs.Query().AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId && r.SiteId == siteId);
            if (run == null)
                return AdminResult<RunDetails>.Missing("Run");

            // The bodies and answers stay in the database here (the page polls this): only whether
            // a step has them; StepExchangeAsync reads one.
            var rows = await _steps.Query().AsNoTracking().Where(s => s.RunId == runId).OrderBy(s => s.Id)
                .Select(s => new
                {
                    Step = new ServerFlowStep
                    {
                        Id = s.Id, RunId = s.RunId, GroupName = s.GroupName, RequestName = s.RequestName, Kind = s.Kind,
                        Attempt = s.Attempt, Status = s.Status, StartedOn = s.StartedOn, EndedOn = s.EndedOn,
                        DurationMs = s.DurationMs, Error = s.Error, Outputs = s.Outputs, DecidedById = s.DecidedById,
                    },
                    HasSent = s.Request != null,
                    HasAnswer = s.Response != null,
                })
                .ToListAsync();
            var steps = rows.Select(r => r.Step).ToList();
            var definition = await _catalog.DefinitionAsync(run.FlowId);

            var progress = definition == null
                ? new List<RequestProgress>()
                : definition.Requests.Select(r => Progress(r, steps.Where(s => Owns(s, r.Name)).ToList(), run)).ToList();

            var names = await NamesAsync(steps.Select(s => s.DecidedById));
            // The flow's hidden values (settings.privacy.hide) never reach this screen.
            var hidden = definition?.Settings.HiddenKeys;
            return AdminResult<RunDetails>.Success(new RunDetails(
                Summary(run),
                run.LastError,
                progress,
                rows.Select(r =>
                {
                    var view = StepView(r.Step);
                    return view with { Outputs = StepPayload.Hide(view.Outputs, hidden), HasSent = r.HasSent, HasAnswer = r.HasAnswer, DecidedByName = NameOf(names, r.Step.DecidedById) };
                }).ToList(),
                StepPayload.Hide(Try(() => JToken.Parse(run.Context ?? "null")), hidden),
                StepPayload.Hide(Try(() => JToken.Parse(run.Outputs ?? "null")), hidden)));
        }

        /// <summary>The hidden values of the flow version a run uses.</summary>
        private async Task<IReadOnlyCollection<string>?> HiddenOfAsync(long flowId) =>
            (await _catalog.DefinitionAsync(flowId))?.Settings.HiddenKeys;

        /// <summary>What one attempt of a run sent and got back.</summary>
        public async Task<AdminResult<StepExchange>> StepExchangeAsync(long siteId, long runId, long stepId)
        {
            var step = await _steps.Query().AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == stepId && s.RunId == runId && s.Run.SiteId == siteId);
            if (step == null)
                return AdminResult<StepExchange>.Missing("Step");
            // Kept masked since the flow hid them; masked again here for what was kept before that.
            var hidden = await HiddenOfAsync(await _runs.Query().AsNoTracking().Where(r => r.Id == runId).Select(r => r.FlowId).FirstOrDefaultAsync());
            return AdminResult<StepExchange>.Success(new StepExchange(step.Id, step.RequestName, step.Kind, step.Attempt, step.Status.ToString(),
                step.StartedOn, step.DurationMs, step.Error, StepPayload.Hide(StepPayload.Read(step.Request), hidden), StepPayload.Hide(StepPayload.Read(step.Response), hidden)));
        }

        /// <summary>The body a stopped request would send next (see <see cref="NextBody"/>).</summary>
        public async Task<AdminResult<NextBody>> NextBodyAsync(long siteId, long runId, string request)
        {
            var run = await _runs.Query().AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId && r.SiteId == siteId);
            if (run == null)
                return AdminResult<NextBody>.Missing("Run");
            if (ServerFlowRetention.IsCleared(run))
                return AdminResult<NextBody>.Fail(Cleared);

            var (definition, steps) = await LoadAsync(run);
            var target = definition?.FindRequest(request);
            if (definition == null || target == null)
                return AdminResult<NextBody>.Fail($"The flow has no request \"{request}\".");

            var why = WhyNotResend(run, definition, steps, target);
            // Hidden values show as "***"; left as they are, ResendAsync puts the real ones back.
            var hidden = definition.Settings.HiddenKeys;

            var edited = ParseObjectOrEmpty(run.RequestOverrides)[target.Name];
            if (edited != null)
                return AdminResult<NextBody>.Success(new NextBody(target.Name, target.Class.ToString(), "edited", StepPayload.Hide(edited, hidden), null, why == null, why));

            var lastSent = steps.LastOrDefault(s => s.RequestName == target.Name && s.Kind == "call" && s.Request != null);
            if (lastSent != null && StepPayload.Read(lastSent.Request) is { } body && !ContainsMasked(body))
                return AdminResult<NextBody>.Success(new NextBody(target.Name, target.Class.ToString(), "lastSent", body, null, why == null, why));

            try
            {
                var built = BuildBody(run, definition, target);
                return AdminResult<NextBody>.Success(new NextBody(target.Name, target.Class.ToString(), "built", StepPayload.Hide(built, hidden), null, why == null, why));
            }
            catch (Exception ex)
            {
                return AdminResult<NextBody>.Success(new NextBody(target.Name, target.Class.ToString(), "built", null, ex.Message, why == null, why));
            }
        }

        /// <summary>
        /// Sends a stopped request again with a body the admin wrote, then lets the run go on with the
        /// requests after it. The body is used until that request succeeds.
        /// </summary>
        public async Task<AdminResult<RunSummary>> ResendAsync(long siteId, long userId, long runId, string request, JToken? body, string? note)
        {
            var run = await _runs.Query().AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId && r.SiteId == siteId);
            if (run == null)
                return AdminResult<RunSummary>.Missing("Run");
            if (ServerFlowRetention.IsCleared(run))
                return AdminResult<RunSummary>.Fail(Cleared);
            if (body == null || body.Type is JTokenType.Null or JTokenType.Undefined)
                return AdminResult<RunSummary>.Fail("Give the body to send.");

            var (definition, steps) = await LoadAsync(run);
            var target = definition?.FindRequest(request);
            if (definition == null || target == null)
                return AdminResult<RunSummary>.Fail($"The flow has no request \"{request}\".");

            // A hidden value the admin did not touch: the real one, from the admin's earlier edit or
            // from what the flow builds now.
            body = body.DeepClone();
            if (ContainsMasked(body))
                body = StepPayload.Restore(body, ParseObjectOrEmpty(run.RequestOverrides)[target.Name]);
            if (ContainsMasked(body))
            {
                try
                {
                    body = StepPayload.Restore(body, BuildBody(run, definition, target));
                }
                catch (Exception)
                {
                    // nothing to put back: the check below refuses the body
                }
            }
            if (ContainsMasked(body))
                return AdminResult<RunSummary>.Fail($"The body still has a hidden value ({StepPayload.Masked}). Write the real value, or remove that field.");

            var why = WhyNotResend(run, definition, steps, target);
            if (why != null)
                return AdminResult<RunSummary>.Fail(why);

            var overrides = ParseObjectOrEmpty(run.RequestOverrides);
            overrides[target.Name] = body.DeepClone();

            // A person's action restarts the request's retry counting.
            return await RequeueAsync(run, userId, $"\"{target.Name}\" sent again with an edited body{Note(note)}",
                ManualStep(run.Id, target, ServerFlowStepStatus.Failed, userId, $"Sent again with a body written by an admin{Note(note)}", null),
                overrides.ToString(Formatting.None), run.Outputs);
        }

        /// <summary>
        /// The site's flows a page can start, with their inputs and the results a page can read: every
        /// active one, plus the newest draft of a name that has no active version yet, so a page can be
        /// set up while its flow waits to be switched on (until then the page gets "no such flow").
        /// </summary>
        public async Task<IReadOnlyList<PageFlowInfo>> PageFlowsAsync(long siteId)
        {
            var active = await _catalog.GetActiveAsync(siteId, FlowSettings.PageTrigger);
            var list = active.Select(f => PageInfo(f.Name, f.Definition, true)).ToList();
            var activeNames = new HashSet<string>(active.Select(f => f.Name), StringComparer.Ordinal);

            // A payment's flows: a payment page reads their run with the order's id.
            foreach (var payment in await _catalog.GetActiveAsync(siteId, FlowSettings.OrderPaidTrigger))
                list.Add(new PageFlowInfo(payment.Name, true, PageTriggerSettings.WhoSignedIn, "order", "orderId", PageTriggerSettings.AfterFailureKeep,
                    payment.Definition.Settings.PaymentPageWaitSec, new[] { new PageFlowInput("orderId", true, "text", null) },
                    payment.Definition.PublicOutputs.Select(o => o.Output).ToList(), FlowSettings.OrderPaidTrigger));

            var drafts = await _flows.Query().AsNoTracking()
                .Where(f => f.SiteId == siteId && f.Status == ServerFlowStatus.Draft && f.Trigger == FlowSettings.PageTrigger)
                .OrderBy(f => f.Name).ThenByDescending(f => f.Version)
                .ToListAsync();
            foreach (var draft in drafts.GroupBy(f => f.Name).Select(g => g.First()).Where(f => !activeNames.Contains(f.Name)))
            {
                var definition = _catalog.Parse(draft).Definition;
                if (definition?.Settings.Page != null)
                    list.Add(PageInfo(draft.Name, definition, false));
            }
            return list.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static PageFlowInfo PageInfo(string name, FlowDefinition definition, bool active)
        {
            var page = definition.Settings.Page!;
            return new PageFlowInfo(name, active, page.Who, page.Once, page.OnceInput, page.AfterFailure, page.WaitSec,
                page.Inputs.Select(i => new PageFlowInput(i.Key, i.Value.Required, i.Value.Type, i.Value.MaxLength)).ToList(),
                definition.PublicOutputs.Select(o => o.Output).ToList());
        }

        /// <summary>
        /// Sends an optional request that failed for good ("given up") again, with the body the flow
        /// builds from the run's data, also when the run has finished. Nothing else is repeated: every
        /// request already done stays done, and the run ends again as soon as this one is settled
        /// (it gets its own retries; if it fails for good again, it is given up again).
        /// </summary>
        public async Task<AdminResult<RunSummary>> SendAgainAsync(long siteId, long userId, long runId, string request, string? note)
        {
            var run = await _runs.Query().AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId && r.SiteId == siteId);
            if (run == null)
                return AdminResult<RunSummary>.Missing("Run");
            if (ServerFlowRetention.IsCleared(run))
                return AdminResult<RunSummary>.Fail(Cleared);

            var (definition, steps) = await LoadAsync(run);
            var target = definition?.FindRequest(request);
            if (definition == null || target == null)
                return AdminResult<RunSummary>.Fail($"The flow has no request \"{request}\".");
            if (!IsGivenUp(steps, target.Name))
                return AdminResult<RunSummary>.Fail($"\"{target.Name}\" was not given up, so there is nothing to send again on its own. Use Try again or Edit and send again.");
            if (IsMoving(run))
                return AdminResult<RunSummary>.Fail($"The run is {run.Status}; wait until it stops.");

            return await RequeueAsync(run, userId, $"\"{target.Name}\" sent again on its own{Note(note)}",
                ManualStep(run.Id, target, ServerFlowStepStatus.Failed, userId, $"Sent again on its own by an admin{Note(note)}", null),
                run.RequestOverrides, run.Outputs);
        }

        /// <summary>Why a request cannot be sent again by hand right now, or null when it can.</summary>
        private static string? WhyNotResend(ServerFlowRun run, FlowDefinition definition, IReadOnlyList<ServerFlowStep> steps, FlowRequest target)
        {
            // An optional request that failed for good can be sent again on its own, also on a
            // finished run: only that request is sent, the ones already done are not repeated.
            if (target.Loop != null)
                return "This call goes over a list (one call per item); it cannot be sent with one edited body. Use Try again: it goes on from the item that stopped.";
            if (IsGivenUp(steps, target.Name))
                return IsMoving(run) ? $"The run is {run.Status}; wait until it stops." : null;

            if (run.Status is not (ServerFlowRunStatus.NeedsAttention or ServerFlowRunStatus.Waiting or ServerFlowRunStatus.Stopped))
                return $"The run is {run.Status}; a request can be sent again only when the run has stopped or waits.";
            if (IsSettled(steps, target.Name))
                return $"\"{target.Name}\" is already finished.";

            var stuck = definition.Requests.FirstOrDefault(r => !IsSettled(steps, r.Name));
            if (stuck != null && stuck.Name != target.Name)
                return $"The run stopped at \"{stuck.Name}\"; only that request can be sent again.";

            var last = steps.LastOrDefault(s => s.RequestName == target.Name);
            if (target.Class == FlowRequestClass.NonIdempotent && last != null && last.Kind != "manual"
                && last.Status is ServerFlowStepStatus.Unclear or ServerFlowStepStatus.Calling)
                return $"Nobody knows yet whether \"{target.Name}\" went through. Check with the other side and record the result first, so it is not done twice.";
            return null;
        }

        /// <summary>The body the flow builds for a request from the run's data now (throws when it cannot).</summary>
        private static JToken BuildBody(ServerFlowRun run, FlowDefinition definition, FlowRequest target)
        {
            var data = new FlowRunData(ParseObjectOrEmpty(run.Context), ParseObjectOrEmpty(run.Outputs), definition,
                FlowRunData.OpsAt(DateTimeOffset.UtcNow, definition.Settings.TimeZone));
            return FlowRunData.JsonSafe(new FlowMappingResolver(data).Prepare(target.DataMapping, target.StaticData, target.MergeData, target.LocalTree)) ?? new JObject();
        }

        private static bool ContainsMasked(JToken token) =>
            token is JValue { Type: JTokenType.String } v ? (string?)v == StepPayload.Masked
            : token.Children().Any(c => ContainsMasked(c is JProperty p ? p.Value : c));

        private static JObject ParseObjectOrEmpty(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new JObject();
            try
            {
                return JsonConvert.DeserializeObject<JToken>(json, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None }) as JObject ?? new JObject();
            }
            catch (JsonException)
            {
                return new JObject();
            }
        }

        /// <summary>
        /// Runs it again from where it stopped, or starts a run that waits for an admin (a payment the
        /// server could not verify). Refused while a nonIdempotent call's result is unknown: record
        /// whether it went through first (Decide), so nothing is issued twice.
        /// </summary>
        public async Task<AdminResult<RunSummary>> RetryAsync(long siteId, long userId, long runId, string? note)
        {
            var run = await _runs.Query().AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId && r.SiteId == siteId);
            if (run == null)
                return AdminResult<RunSummary>.Missing("Run");
            if (ServerFlowRetention.IsCleared(run))
                return AdminResult<RunSummary>.Fail(Cleared);
            if (run.Status == ServerFlowRunStatus.AwaitingApproval)
                return await RequeueAsync(run, userId, $"Started by an admin{Note(note)}", null, run.RequestOverrides, run.Outputs);
            if (run.Status is not (ServerFlowRunStatus.NeedsAttention or ServerFlowRunStatus.Waiting or ServerFlowRunStatus.Stopped))
                return AdminResult<RunSummary>.Fail($"A {run.Status} run cannot be retried.");

            var (definition, steps) = await LoadAsync(run);
            if (definition == null)
                return AdminResult<RunSummary>.Fail("The run's flow version does not pass the checks any more.");

            ServerFlowStep? fresh = null;
            var stuck = definition.Requests.FirstOrDefault(r => !IsSettled(steps, r.Name) && steps.Any(s => Owns(s, r.Name)));
            if (stuck != null)
            {
                var last = steps.Last(s => Owns(s, stuck.Name));
                if (stuck.Class == FlowRequestClass.NonIdempotent && stuck.Guard == null
                    && last.Kind != "manual" && last.Status is ServerFlowStepStatus.Unclear or ServerFlowStepStatus.Calling)
                    return AdminResult<RunSummary>.Fail($"Nobody knows yet whether \"{stuck.Name}\" went through. Check with the other side and record the result (went through / did not go through) instead of retrying.");

                // A fresh start for the request's retry counting.
                if (stuck.Class != FlowRequestClass.NonIdempotent || last.Status == ServerFlowStepStatus.Failed)
                    fresh = ManualStep(run.Id, stuck, ServerFlowStepStatus.Failed, userId, $"Retry requested{Note(note)}", null);
            }

            return await RequeueAsync(run, userId, $"Retried by an admin{Note(note)}", fresh, run.RequestOverrides, run.Outputs);
        }

        /// <summary>
        /// Records what a person found out about a request whose result was unclear (or any stopped
        /// request): it went through (with its output values), or it did not (it will be sent again).
        /// </summary>
        public async Task<AdminResult<RunSummary>> DecideAsync(long siteId, long userId, long runId, string request, bool wentThrough, JObject? outputs, string? note)
        {
            var run = await _runs.Query().AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId && r.SiteId == siteId);
            if (run == null)
                return AdminResult<RunSummary>.Missing("Run");
            if (ServerFlowRetention.IsCleared(run))
                return AdminResult<RunSummary>.Fail(Cleared);
            if (run.Status is ServerFlowRunStatus.Running or ServerFlowRunStatus.Succeeded or ServerFlowRunStatus.Queued)
                return AdminResult<RunSummary>.Fail($"The run is {run.Status}; decide only on a stopped or waiting run.");

            var (definition, steps) = await LoadAsync(run);
            var target = definition?.FindRequest(request);
            if (target == null)
                return AdminResult<RunSummary>.Fail($"The flow has no request \"{request}\".");
            if (IsSettled(steps, target.Name))
                return AdminResult<RunSummary>.Fail($"\"{request}\" is already finished.");

            JObject? values = null;
            var keptOutputs = run.Outputs;
            if (wentThrough)
            {
                values = new JObject();
                foreach (var name in target.Outputs.Keys)
                {
                    var value = outputs?[name];
                    if (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.String && string.IsNullOrWhiteSpace((string?)value)))
                        return AdminResult<RunSummary>.Fail($"Give the value of \"{name}\" (the rest of the flow uses it).");
                    values[name] = value.DeepClone();
                }

                // Keep the run's outputs in step with the decision.
                var saved = ParseObjectOrEmpty(run.Outputs);
                saved[target.Name] = values.DeepClone();
                keptOutputs = saved.ToString(Formatting.None);
            }

            return await RequeueAsync(run, userId, $"Decision recorded for \"{request}\"{Note(note)}",
                ManualStep(run.Id, target, wentThrough ? ServerFlowStepStatus.Succeeded : ServerFlowStepStatus.Failed, userId,
                    (wentThrough ? "Went through" : "Did not go through") + Note(note), values),
                run.RequestOverrides, keptOutputs);
        }

        public async Task<AdminResult<RunSummary>> StopAsync(long siteId, long userId, long runId, string? note)
        {
            var run = await _runs.Query().AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId && r.SiteId == siteId);
            if (run == null)
                return AdminResult<RunSummary>.Missing("Run");
            if (run.Status == ServerFlowRunStatus.Succeeded)
                return AdminResult<RunSummary>.Fail("A finished run cannot be stopped.");
            var now = DateTimeOffset.UtcNow;
            if (run.Status == ServerFlowRunStatus.Running && run.LeaseUntil > now)
                return AdminResult<RunSummary>.Fail("The run is working right now; stop it when it waits or stops.");

            // Only if it is still as it was read (status and last change), and no worker holds it.
            var (id, status, seen) = (run.Id, run.Status, run.UpdatedOn);
            var why = StepPayload.Fit($"Stopped by an admin{Note(note)}", ServerFlowRun.ReasonLength);
            var changed = await _runs.Query()
                .Where(r => r.Id == id && r.Status == status && r.UpdatedOn == seen && (r.LeaseUntil == null || r.LeaseUntil < now))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, ServerFlowRunStatus.Stopped)
                    .SetProperty(r => r.Reason, why)
                    .SetProperty(r => r.NextAttemptAt, (DateTimeOffset?)null)
                    .SetProperty(r => r.LeaseOwner, (string?)null)
                    .SetProperty(r => r.LeaseUntil, (DateTimeOffset?)null)
                    .SetProperty(r => r.LastActionById, userId)
                    .SetProperty(r => r.UpdatedOn, now)
                    // finished from now: the retention counts its keep time from here
                    .SetProperty(r => r.CompletedOn, now));
            if (changed == 0)
                return AdminResult<RunSummary>.Fail(MovedOn);
            _signals.Pulse(run.SubjectId);
            return AdminResult<RunSummary>.Success(Summary(await _runs.Query().AsNoTracking().FirstAsync(r => r.Id == id)));
        }

        /// <summary>Why an admin's action on a run was not done: it changed since it was opened.</summary>
        private const string MovedOn = "The run moved on since you opened it (it started, waits for something else, or another admin acted on it). Refresh it and look again.";

        /// <summary>Why a run cleared by the retention cannot be acted on.</summary>
        private const string Cleared = "This run was cleared after its flow's keep time (privacy.keepRunsDays): only its row is left, so it cannot be run or changed any more.";

        /// <summary>
        /// Starts a stopped, waiting or approval-waiting run again for an admin, only if it is still as
        /// it was read for this action: the same status and the same last change (UpdatedOn: a run
        /// that moved on and came back to the same status has another one), no worker holding it, not
        /// cleared. The outputs and edited bodies written are the ones read. The person's step goes in
        /// with it, in one transaction, so the worker that picks the run up next sees it.
        /// </summary>
        private async Task<AdminResult<RunSummary>> RequeueAsync(ServerFlowRun run, long userId, string reason, ServerFlowStep? step, string? requestOverrides, string? outputs)
        {
            var (id, status, seen) = (run.Id, run.Status, run.UpdatedOn);
            var now = DateTimeOffset.UtcNow;
            var why = StepPayload.Fit(reason, ServerFlowRun.ReasonLength);
            var changed = 0;
            await _runs.ExecuteInTransactionAsync(async () =>
            {
                changed = await _runs.Query()
                    .Where(r => r.Id == id && r.Status == status && r.UpdatedOn == seen && (r.LeaseUntil == null || r.LeaseUntil < now) && r.Context != null)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.Status, ServerFlowRunStatus.Queued)
                        .SetProperty(r => r.NextAttemptAt, (DateTimeOffset?)null)
                        .SetProperty(r => r.LeaseOwner, (string?)null)
                        .SetProperty(r => r.LeaseUntil, (DateTimeOffset?)null)
                        // A person looked at it: give it a fresh allowance of pickups.
                        .SetProperty(r => r.Attempts, 0)
                        .SetProperty(r => r.Reason, why)
                        .SetProperty(r => r.RequestOverrides, requestOverrides)
                        .SetProperty(r => r.Outputs, outputs)
                        .SetProperty(r => r.LastActionById, userId)
                        .SetProperty(r => r.UpdatedOn, now));
                if (changed == 1 && step != null)
                {
                    _steps.Add(step);
                    await _steps.SaveChangesAsync();
                }
            });
            if (changed == 0)
                return AdminResult<RunSummary>.Fail(MovedOn);

            _queue.Enqueue(id);
            _signals.Pulse(run.SubjectId);
            return AdminResult<RunSummary>.Success(Summary(await _runs.Query().AsNoTracking().FirstAsync(r => r.Id == id)));
        }

        #endregion

        #region Helpers

        private FlowParseResult Parse(string definition) =>
            FlowDefinitionParser.Parse(definition, _options.CurrentValue.TimeZoneId, _options.CurrentValue.MaxDefinitionLength);

        /// <summary>
        /// Issues only this server can see: missing tokens, unknown data sources, writes a browser could
        /// call, order statuses DCMS does not have (or paid ones), a webhook address the server does not
        /// let through, and an e-mail template the site does not have.
        /// </summary>
        private async Task<IReadOnlyList<FlowIssue>> ServerIssuesAsync(long siteId, FlowDefinition definition)
        {
            var issues = new List<FlowIssue>();

            foreach (var token in FlowDefinitionParser.TokenNamesOf(definition))
            {
                if (string.IsNullOrEmpty(_configuration[$"{FlowCaller.TokensSection}:{token}"]))
                    issues.Add(FlowIssue.Warning("", "TOKEN_NOT_SET", $"The token \"{token}\" is not set on this server ({FlowCaller.TokensSection}:{token})."));
            }

            var ids = definition.Requests.Where(r => r.Kind == FlowRequestKind.DataSource).Select(r => r.DataSourceId)
                .Concat(definition.Requests.Where(r => r.Guard != null).Select(r => r.Guard!.DataSourceId))
                .Distinct().ToList();
            var known = await _dataSources.Query().AsNoTracking()
                .Where(d => ids.Contains(d.Id) && (d.SiteId == null || d.SiteId == siteId))
                .Select(d => d.Id).ToListAsync();
            // An error, not a warning: saving switches the flow on, and every run would then stop on
            // this call ("Data Source Not configured").
            foreach (var missing in ids.Except(known))
                issues.Add(FlowIssue.Error("", "UNKNOWN_DATA_SOURCE", $"Data source {missing} does not exist on this site. Use the id of an existing data source."));

            var serverOnly = _configuration.GetSection("DataSources:ServerOnlyIds").Get<long[]>() ?? Array.Empty<long>();
            foreach (var write in definition.Requests.Where(r => r.Kind == FlowRequestKind.DataSource && r.Class != FlowRequestClass.Read && !serverOnly.Contains(r.DataSourceId)))
                issues.Add(FlowIssue.Warning("", "NOT_SERVER_ONLY",
                    $"\"{write.Name}\" changes data through data source {write.DataSourceId}, which browsers can still call directly. Add {write.DataSourceId} to DataSources:ServerOnlyIds in the server configuration."));

            if (definition.Settings.OrderStatus is { } orderStatus)
                foreach (var name in orderStatus.ToStatuses)
                {
                    if (!Enum.TryParse<OrderStatus>(name, true, out var status) || !Enum.IsDefined(status))
                        issues.Add(FlowIssue.Error("settings.trigger.toStatuses", "UNKNOWN_STATUS",
                            $"\"{name}\" is not an order status. The statuses are: {string.Join(", ", ServerFlowOrderStatuses.Offered)}."));
                    else if (!ServerFlowOrderStatuses.Offered.Contains(status))
                        issues.Add(FlowIssue.Error("settings.trigger.toStatuses", "PAID_STATUS",
                            $"\"{name}\" means the order was paid: use \"After a payment\" for it, which also checks the payment."));
                }

            // /Order/ChangeStatus: its fixed statuses are DCMS's, and a paid status checks the amount.
            foreach (var request in definition.Requests.Where(r => r.Kind == FlowRequestKind.Local && r.Operation == ServerOperations.ChangeOrderStatus))
            {
                OrderStatus? Known(string field)
                {
                    var value = FlowDefinitionParser.FixedValueOf(request, field);
                    if (value == null)
                        return null;
                    if (Enum.TryParse<OrderStatus>(value, true, out var status) && Enum.IsDefined(status))
                        return status;
                    issues.Add(FlowIssue.Error("", "UNKNOWN_STATUS",
                        $"\"{request.Name}\": {field} \"{value}\" is not an order status. The statuses are: {string.Join(", ", Enum.GetNames<OrderStatus>())}."));
                    return null;
                }
                var from = Known("fromStatus");
                var to = Known("toStatus");
                if (from != null && from == to)
                    issues.Add(FlowIssue.Error("", "SAME_STATUS", $"\"{request.Name}\" moves orders from {from} to {to}: the same status."));
                if (to is OrderStatus.PaymentReceived or OrderStatus.PaidByCoupon && !FlowDefinitionParser.HasField(request, "amount"))
                    issues.Add(FlowIssue.Error("", "AMOUNT_NEEDED",
                        $"\"{request.Name}\" marks orders paid ({to}), so it needs amount: the amount paid. The server moves the order only when it is the order's total."));
            }

            if (definition.Settings.Webhook is { Signed: true, KeyUntil: { } keyUntil } signedHook && signedHook.KeyHash.Length > 0)
            {
                // the day in the flow's time zone, as the webhook checks it
                var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, definition.Settings.TimeZone).DateTime);
                if (keyUntil < today)
                    issues.Add(FlowIssue.Warning("settings.trigger.keyUntil", "KEY_UNTIL_PASSED",
                        $"The old key was taken through {keyUntil:yyyy-MM-dd} and is not taken any more. Remove it (and keyUntil), or set a later day."));
                else if (keyUntil.DayNumber - today.DayNumber > WebhookAuth.MaxKeyDays)
                    issues.Add(FlowIssue.Error("settings.trigger.keyUntil", "KEY_UNTIL_FAR",
                        $"The old key may be taken at most {WebhookAuth.MaxKeyDays} days more (through {today.AddDays(WebhookAuth.MaxKeyDays):yyyy-MM-dd}): it is the weaker proof, kept only while the other system moves over to signing."));
            }

            if (definition.Settings.Webhook != null)
            {
                var (open, unwrapped) = ServerFlowWebhooks.AddressReadiness(_configuration);
                if (!open)
                    issues.Add(FlowIssue.Warning("settings.trigger", "WEBHOOK_NOT_OPEN",
                        $"Other systems cannot call this flow yet: the server asks every request for DCMS's signature. Add \"{ServerFlowWebhooks.PathPrefix}\" to CypherSecurity:ExecludedApis and CypherSecurity:ApiWrapperExecludedApis in the server's appsettings.json."));
                else if (!unwrapped)
                    issues.Add(FlowIssue.Warning("settings.trigger", "WEBHOOK_WRAPPED",
                        $"The server wraps this flow's answers in DCMS's envelope. Add \"{ServerFlowWebhooks.PathPrefix}\" to CypherSecurity:ApiWrapperExecludedApis so other systems get {{ runId, status, results, message }} as it is."));
            }

            if (definition.Settings.AttentionTemplateId is { } template
                && !await _templates.Query().AsNoTracking().AnyAsync(t => t.Id == template && t.SiteId == siteId && t.Status == EStatus.Active))
                issues.Add(FlowIssue.Warning("settings.notify.onAttention.template", "UNKNOWN_TEMPLATE",
                    $"The site has no e-mail template \"{template}\" (Email templates): until it has, the e-mail a stopped run sends uses the built-in text."));

            return issues;
        }

        private static RequestProgress Progress(FlowRequest request, IReadOnlyList<ServerFlowStep> steps, ServerFlowRun run)
        {
            var last = steps.LastOrDefault();
            var state = last == null ? "pending" : last.Status switch
            {
                ServerFlowStepStatus.Succeeded => "done",
                ServerFlowStepStatus.Skipped => "skipped",
                ServerFlowStepStatus.Calling => "calling",
                ServerFlowStepStatus.Unclear => "unclear",
                _ => "failed",
            };
            var stopped = run.Status is ServerFlowRunStatus.NeedsAttention or ServerFlowRunStatus.Waiting or ServerFlowRunStatus.Stopped;
            var canDecide = stopped && last != null && state is "unclear" or "failed" or "calling";
            var canSendAgain = IsGivenUp(steps, request.Name) && !IsMoving(run);
            return new RequestProgress(request.GroupName, request.Name, request.Class.ToString(), request.Critical, state, request.Outputs.Keys.ToList(), canDecide, canSendAgain);
        }

        private async Task<(FlowDefinition? Definition, List<ServerFlowStep> Steps)> LoadAsync(ServerFlowRun run)
        {
            var definition = await _catalog.DefinitionAsync(run.FlowId);
            var steps = await _steps.Query().AsNoTracking().Where(s => s.RunId == run.Id).OrderBy(s => s.Id).ToListAsync();
            return (definition, steps);
        }

        /// <summary>An optional request that failed for good (its last step is the runner's "given up").</summary>
        private static bool IsGivenUp(IReadOnlyList<ServerFlowStep> steps, string request)
        {
            var last = steps.LastOrDefault(s => s.RequestName == request);
            return last != null && last.Status == ServerFlowStepStatus.Skipped && (last.Error ?? "").StartsWith(ServerFlowRunner.GivenUp, StringComparison.Ordinal);
        }

        /// <summary>A step of the request, or of one of its loop items ("name[0]", ...).</summary>
        private static bool Owns(ServerFlowStep step, string request) =>
            step.RequestName == request
            || (step.RequestName.Length > request.Length && step.RequestName[request.Length] == '[' && step.RequestName.StartsWith(request, StringComparison.Ordinal));

        /// <summary>A worker has it or is about to take it: no hand action until it stops.</summary>
        private static bool IsMoving(ServerFlowRun run) => run.Status is ServerFlowRunStatus.Queued or ServerFlowRunStatus.Running;

        private static bool IsSettled(IReadOnlyList<ServerFlowStep> steps, string request)
        {
            var last = steps.LastOrDefault(s => s.RequestName == request);
            return last != null && last.Status is ServerFlowStepStatus.Succeeded or ServerFlowStepStatus.Skipped;
        }

        private static ServerFlowStep ManualStep(long runId, FlowRequest request, ServerFlowStepStatus status, long userId, string text, JObject? outputs)
        {
            return new ServerFlowStep
            {
                RunId = runId,
                GroupName = StepPayload.Fit(request.GroupName, ServerFlowRun.NameLength),
                RequestName = StepPayload.Fit(request.Name, ServerFlowRun.NameLength),
                Kind = "manual",
                Attempt = 0,
                Status = status,
                EndedOn = DateTimeOffset.UtcNow,
                Error = StepPayload.Fit(text, ServerFlowRun.ErrorLength),
                Outputs = outputs?.ToString(Formatting.None),
                DecidedById = userId,
            };
        }

        private static FlowVersionInfo Info(ServerFlow f) => new(f.Id, f.Name, f.Version, f.Status.ToString(), f.Trigger, f.Note,
            f.CreatedOn, f.CreatedById, f.ActivatedOn, f.ActivatedById, f.Checksum);

        private static RunSummary Summary(ServerFlowRun r) => new(r.Id, r.FlowName, r.FlowId, r.SubjectType, r.SubjectId, r.Status.ToString(),
            r.CurrentStep, r.Reference, r.Reason, r.Attempts, r.NextAttemptAt, r.CreatedOn, r.UpdatedOn, r.CompletedOn);

        private static StepInfo StepView(ServerFlowStep s) => new(s.Id, s.GroupName, s.RequestName, s.Kind, s.Attempt, s.Status.ToString(),
            s.StartedOn, s.EndedOn, s.DurationMs, s.Error, Try(() => s.Outputs == null ? null : JToken.Parse(s.Outputs)), s.DecidedById);

        public static string Checksum(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

        private static string Note(string? note) => string.IsNullOrWhiteSpace(note) ? "" : $": {note.Trim()}";

        private static T? Try<T>(Func<T?> read) where T : class
        {
            try { return read(); }
            catch (JsonException) { return null; }
        }

        #endregion
    }
}
