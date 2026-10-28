using EskaCMS.Entities.Entity.ServerFlows;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.IntegrationDepartment.ServerFlows.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Controllers
{
    public sealed class FlowBody
    {
        public string? Name { get; set; }
        public string? Note { get; set; }

        /// <summary>The flow JSON ({ settings, apiGroupList }), as an object or as JSON text.</summary>
        public JToken? Definition { get; set; }
    }

    public sealed class ActivateBody
    {
        public string? Checksum { get; set; }
    }

    public sealed class NoteBody
    {
        public string? Note { get; set; }
    }

    public sealed class DecideBody
    {
        public string? Request { get; set; }
        public bool WentThrough { get; set; }
        public JObject? Outputs { get; set; }
        public string? Note { get; set; }
    }

    public sealed class SchedulePreviewBody
    {
        /// <summary>The schedule trigger: { type: "schedule", everyMinutes | times, days }.</summary>
        public JToken? Trigger { get; set; }
        public string? TimeZone { get; set; }
    }

    public sealed class PublicKeyBody
    {
        /// <summary>What the admin pasted: PEM, a certificate, a JWK or base64.</summary>
        public string? Text { get; set; }
    }

    public sealed class ResendBody
    {
        public string? Request { get; set; }

        /// <summary>The exact body to send (what "What was sent" shows for an attempt).</summary>
        public JToken? Body { get; set; }

        public string? Note { get; set; }
    }

    /// <summary>
    /// Admin API for server flows: save, check, try and activate flow versions; follow runs, retry
    /// them, record decisions, start and stop them. Admins of the site in the siteId header only.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/ServerFlows/admin")]
    [ServiceFilter(typeof(ServerFlowAdminFilter))]
    public sealed class ServerFlowsAdminController : ControllerBase
    {
        private readonly ServerFlowAdminService _service;
        private readonly ServerFlowAdminTools _tools;
        private readonly ServerFlowTryIt _tryIt;
        private readonly ServerFlowWebhookCalls _webhookCalls;

        public ServerFlowsAdminController(ServerFlowAdminService service, ServerFlowAdminTools tools, ServerFlowTryIt tryIt, ServerFlowWebhookCalls webhookCalls)
        {
            _service = service;
            _tools = tools;
            _tryIt = tryIt;
            _webhookCalls = webhookCalls;
        }

        private ServerFlowAdmin Admin => (ServerFlowAdmin)HttpContext.Items[ServerFlowAdminFilter.ItemKey]!;

        [HttpGet("flows")]
        public async Task<IActionResult> ListFlows() => Ok(await _service.ListFlowsAsync(Admin.SiteId));

        [HttpGet("flows/{id:long}")]
        public async Task<IActionResult> GetFlow(long id) => Answer(await _service.GetFlowAsync(Admin.SiteId, id));

        /// <summary>Checks a flow without saving it.</summary>
        [HttpPost("flows/validate")]
        public async Task<IActionResult> Validate([FromBody] FlowBody body)
        {
            var issues = await _service.ValidateAsync(Admin.SiteId, DefinitionText(body.Definition));
            return Ok(new { valid = issues.All(i => i.Severity != FlowIssueSeverity.Error), issues });
        }

        /// <summary>Saves a new draft version (version numbers count up per name).</summary>
        [HttpPost("flows")]
        public async Task<IActionResult> Save([FromBody] FlowBody body) =>
            Answer(await _service.SaveAsync(Admin.SiteId, Admin.UserId, body.Name ?? "", body.Note, DefinitionText(body.Definition)));

        [HttpPost("flows/{id:long}/activate")]
        public async Task<IActionResult> Activate(long id, [FromBody] ActivateBody body) =>
            Answer(await _service.ActivateAsync(Admin.SiteId, Admin.UserId, id, body.Checksum));

        [HttpPost("flows/{id:long}/archive")]
        public async Task<IActionResult> Archive(long id) => Answer(await _service.ArchiveAsync(Admin.SiteId, Admin.UserId, id));

        /// <summary>
        /// "Try it": simulates the version with the data its trigger would give (calls nothing, writes
        /// nothing), or with run = true starts a real run of the version that is on, as its trigger
        /// would: after a payment, an order's status, or a schedule (pages and other systems start
        /// their own flows).
        /// </summary>
        [HttpPost("flows/{id:long}/try")]
        public async Task<IActionResult> Try(long id, [FromBody] TryBody body) =>
            Answer(await _tryIt.TryAsync(Admin.SiteId, Admin.UserId, id, body ?? new TryBody(), HttpContext.RequestAborted));

        /// <summary>The active flows a page can start: what each accepts and gives back (the request editor's picker).</summary>
        [HttpGet("page-flows")]
        public async Task<IActionResult> PageFlows() => Ok(await _service.PageFlowsAsync(Admin.SiteId));

        /// <summary>
        /// The flow editor's choices, from the server and the site: default time zone, languages and
        /// translations of the sentences callers get, order statuses, payment gateways, the webhook
        /// address and the server's limits.
        /// </summary>
        [HttpGet("settings")]
        public async Task<IActionResult> Settings() => Ok(await _tools.SettingsAsync(Admin.SiteId, HttpContext.RequestAborted));

        /// <summary>A new webhook key and its hash: the editor writes the hash into the flow and shows the key once.</summary>
        [HttpPost("webhook-key")]
        public IActionResult WebhookKey()
        {
            NoStore();
            var (key, keyHash) = ServerFlowWebhooks.NewKey();
            return Ok(new { key, keyHash });
        }

        /// <summary>
        /// A new key pair (ECDSA P-256) for another system that signs its requests: the editor adds the
        /// public key to the flow and shows the private key once (PEM, and a JWK for platforms without
        /// PEM support). The server keeps nothing of the private key.
        /// </summary>
        [HttpPost("webhook-keypair")]
        public IActionResult WebhookKeyPair()
        {
            NoStore();
            var (privateKeyPem, privateKeyJwk, publicKey) = WebhookAuth.NewKeyPair();
            return Ok(new { privateKeyPem, privateKeyJwk, publicKey = Describe(publicKey) });
        }

        /// <summary>Reads the other system's public key from what the admin pasted (PEM, a certificate, a JWK or base64), or says why it cannot.</summary>
        [HttpPost("webhook-public-key")]
        public IActionResult WebhookPublicKey([FromBody] PublicKeyBody body)
        {
            var parsed = WebhookAuth.ParsePublicKey(body?.Text, out var problem);
            return parsed == null ? BadRequest(new { error = problem }) : Ok(Describe(parsed));
        }

        /// <summary>A webhook flow's last calls on this server, newest first (kept in memory; a restart clears them).</summary>
        [HttpGet("webhook-calls")]
        public IActionResult WebhookCalls([FromQuery] string? flow) => Ok(_webhookCalls.For(Admin.SiteId, flow ?? ""));

        private static object Describe(ParsedPublicKey key) => new
        {
            key = key.Base64,
            type = key.Type,
            fingerprint = key.Fingerprint,
            shortFingerprint = WebhookAuth.Short(key.Fingerprint),
            pem = WebhookAuth.PublicKeyPem(key.Spki),
        };

        /// <summary>Keys are shown once: no cache along the way may keep the answer.</summary>
        private void NoStore()
        {
            Response.Headers.CacheControl = "no-store";
            Response.Headers.Pragma = "no-cache";
        }

        /// <summary>The next times a schedule runs (only the trigger and the time zone are read).</summary>
        [HttpPost("schedule-preview")]
        public IActionResult SchedulePreview([FromBody] SchedulePreviewBody body)
        {
            var (next, issues) = _tools.SchedulePreview(body.Trigger, body.TimeZone);
            return Ok(new { next, issues });
        }

        /// <summary>
        /// The site's runs, newest first. status: one status or several, comma-separated; search: a
        /// number (the run's, or the order, customer or cart it is for) or part of a reference; flowName;
        /// from / to: when it started (ISO dates with their offset; to is exclusive).
        /// </summary>
        [HttpGet("runs")]
        public async Task<IActionResult> ListRuns([FromQuery] string? status, [FromQuery] string? search, [FromQuery] string? flowName,
            [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (!TryStatuses(status, out var statuses, out var unknown))
                return BadRequest(new { error = $"\"{unknown}\" is not a run status." });
            var (items, total) = await _service.ListRunsAsync(Admin.SiteId, new RunFilter(statuses, search, flowName, from, to), page, pageSize);
            return Ok(new { items, total, page, pageSize });
        }

        /// <summary>How many runs match search / flowName / from / to, per status: { NeedsAttention: 3, ... }.</summary>
        [HttpGet("runs/counts")]
        public async Task<IActionResult> CountRuns([FromQuery] string? search, [FromQuery] string? flowName, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to) =>
            Ok(await _service.CountRunsAsync(Admin.SiteId, new RunFilter(Array.Empty<ServerFlowRunStatus>(), search, flowName, from, to)));

        private static bool TryStatuses(string? text, out IReadOnlyList<ServerFlowRunStatus> statuses, out string? unknown)
        {
            var list = new List<ServerFlowRunStatus>();
            unknown = null;
            foreach (var part in (text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Enum.TryParse<ServerFlowRunStatus>(part, true, out var s) || !Enum.IsDefined(s))
                {
                    unknown = part;
                    statuses = list;
                    return false;
                }
                list.Add(s);
            }
            statuses = list;
            return true;
        }

        [HttpGet("runs/{id:long}")]
        public async Task<IActionResult> GetRun(long id) => Answer(await _service.GetRunAsync(Admin.SiteId, id));

        /// <summary>Runs it again from where it stopped; a run waiting for an admin (an unverified payment) starts.</summary>
        [HttpPost("runs/{id:long}/retry")]
        public async Task<IActionResult> Retry(long id, [FromBody] NoteBody? body) =>
            Answer(await _service.RetryAsync(Admin.SiteId, Admin.UserId, id, body?.Note));

        /// <summary>Records whether a request went through (with its output values) or not.</summary>
        [HttpPost("runs/{id:long}/decide")]
        public async Task<IActionResult> Decide(long id, [FromBody] DecideBody body) =>
            Answer(await _service.DecideAsync(Admin.SiteId, Admin.UserId, id, body.Request ?? "", body.WentThrough, body.Outputs, body.Note));

        /// <summary>What one attempt sent and got back.</summary>
        [HttpGet("runs/{id:long}/steps/{stepId:long}")]
        public async Task<IActionResult> Step(long id, long stepId) => Answer(await _service.StepExchangeAsync(Admin.SiteId, id, stepId));

        /// <summary>The body a stopped request would send next: an edited one, the last one sent, or a freshly built one.</summary>
        [HttpGet("runs/{id:long}/next-body")]
        public async Task<IActionResult> NextBody(long id, [FromQuery] string? request) =>
            Answer(await _service.NextBodyAsync(Admin.SiteId, id, request ?? ""));

        /// <summary>Sends the stopped request again with this body; the run then goes on.</summary>
        [HttpPost("runs/{id:long}/resend")]
        public async Task<IActionResult> Resend(long id, [FromBody] ResendBody body) =>
            Answer(await _service.ResendAsync(Admin.SiteId, Admin.UserId, id, body.Request ?? "", body.Body, body.Note));

        /// <summary>Sends an optional request that was given up again, on its own (also on a finished run).</summary>
        [HttpPost("runs/{id:long}/send-again")]
        public async Task<IActionResult> SendAgain(long id, [FromBody] ResendBody body) =>
            Answer(await _service.SendAgainAsync(Admin.SiteId, Admin.UserId, id, body.Request ?? "", body.Note));

        [HttpPost("runs/{id:long}/stop")]
        public async Task<IActionResult> Stop(long id, [FromBody] NoteBody? body) =>
            Answer(await _service.StopAsync(Admin.SiteId, Admin.UserId, id, body?.Note));

        private IActionResult Answer<T>(AdminResult<T> result)
        {
            if (result.NotFound)
                return NotFound(new { error = result.Error });
            if (!result.Ok)
                return BadRequest(new { error = result.Error, issues = result.Issues });
            return Ok(new { value = result.Value, issues = result.Issues });
        }

        /// <summary>The definition as stored text: an object is written out indented; JSON text is kept as sent.</summary>
        private static string DefinitionText(JToken? definition) => definition switch
        {
            null => "",
            { Type: JTokenType.String } => (string?)definition ?? "",
            _ => definition.ToString(Formatting.Indented),
        };
    }
}
