using System.Globalization;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCMS.IntegrationDepartment.ServerFlows.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Controllers
{
    /// <summary>
    /// The address another system calls to start a flow whose trigger is "webhook". No sign-in: the
    /// call proves it is the other system's with a signature (X-Flow-Timestamp, X-Flow-Signature) or
    /// the flow's key (X-Flow-Key), checked by <see cref="ServerFlowWebhooks"/>. The body is read as
    /// the exact bytes sent, which a signature covers.
    /// The server's CypherSecurity must let /api/ServerFlows/hook/ through unsigned (ExecludedApis),
    /// as the Admin's "When it runs" tab says when it does not.
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("api/ServerFlows/hook/{siteId:long}/{flow}")]
    public sealed class ServerFlowsHookController : ControllerBase
    {
        private const int MaxBodyBytes = 64 * 1024;

        private readonly ServerFlowWebhooks _webhooks;

        public ServerFlowsHookController(ServerFlowWebhooks webhooks)
        {
            _webhooks = webhooks;
        }

        /// <summary>
        /// Starts a run with the JSON object sent (or answers about the run of the same value, for a
        /// flow that runs once per value), waiting up to the flow's waitSec; ?wait= overrides it.
        /// Answers { runId, status, results, message, waitSec }.
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(MaxBodyBytes)]
        public async Task<IActionResult> Call(long siteId, string flow, [FromQuery] int? wait)
        {
            var body = await ReadBodyAsync();
            if (body == null)
                return StatusCode(413, new { error = new CallerText(FlowTexts.InputTooLarge, MaxBodyBytes / 1024).ToString() });
            return this.ToResponse(await _webhooks.CallAsync(siteId, flow, RequestOf("POST", body), CultureId, wait, HttpContext.RequestAborted));
        }

        /// <summary>The state and results of a run the flow's caller started; ?wait=10 waits up to 10 s for it to finish.</summary>
        [HttpGet("run/{runId:long}")]
        public async Task<IActionResult> GetRun(long siteId, string flow, long runId, [FromQuery] int? wait) =>
            this.ToResponse(await _webhooks.GetAsync(siteId, flow, RequestOf("GET", Array.Empty<byte>()), CultureId, runId, wait, HttpContext.RequestAborted));

        private WebhookRequest RequestOf(string method, byte[] body) => new(
            method,
            Header(ServerFlowWebhooks.KeyHeader),
            Header(WebhookAuth.TimestampHeader),
            Header(WebhookAuth.SignatureHeader),
            body,
            HttpContext.Connection.RemoteIpAddress,
            Header("CF-Connecting-IP"),
            (Request.PathBase + Request.Path).Value);

        private string? Header(string name) => Request.Headers[name].ToString() is { Length: > 0 } value ? value.Trim() : null;

        /// <summary>The body's bytes exactly as sent; null when it is larger than MaxBodyBytes.</summary>
        private async Task<byte[]?> ReadBodyAsync()
        {
            using var copy = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await Request.Body.ReadAsync(buffer, HttpContext.RequestAborted)) > 0)
            {
                if (copy.Length + read > MaxBodyBytes)
                    return null;
                copy.Write(buffer, 0, read);
            }
            return copy.ToArray();
        }

        /// <summary>The language the caller wants its sentences in (a site language id), else the site's default.</summary>
        private long CultureId =>
            long.TryParse(Request.Headers["cultureId"].ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : 0;
    }

    internal static class PageRunResponses
    {
        /// <summary>{ runId, status, results, message, waitSec }, or { error } with the refusal's status code.</summary>
        public static IActionResult ToResponse(this ControllerBase controller, PageRunResult result)
        {
            if (result.Answer == null)
                return controller.StatusCode(result.StatusCode, new { error = result.Error });
            var a = result.Answer;
            return controller.Ok(new { runId = a.RunId, status = a.Status, results = a.Results, message = a.Message, waitSec = a.WaitSec });
        }
    }
}
