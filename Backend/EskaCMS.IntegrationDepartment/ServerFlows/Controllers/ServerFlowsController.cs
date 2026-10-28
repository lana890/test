using System.Globalization;
using System.Security.Claims;
using EskaCMS.IntegrationDepartment.ServerFlows.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Controllers
{
    /// <summary>{ flow, input, wait }: start (or join) a run of a flow a page may start.</summary>
    public sealed record PageRunBody(string? Flow, JToken? Input, int? Wait);

    /// <summary>What the customer's pages may ask about server flows.</summary>
    [ApiController]
    [Authorize]
    [Route("api/ServerFlows")]
    public sealed class ServerFlowsController : ControllerBase
    {
        /// <summary>A page sends a few named values; nothing it may send comes near this.</summary>
        private const int MaxBodyBytes = 64 * 1024;

        private readonly ServerFlowPageRuns _pageRuns;

        public ServerFlowsController(ServerFlowPageRuns pageRuns)
        {
            _pageRuns = pageRuns;
        }

        /// <summary>
        /// "Run on the server" from a page: starts the flow for the signed-in visitor (or answers
        /// about the run it already has, when the flow runs once per journey / customer / input), then
        /// waits for the result as the flow says (waitSec), at most ServerFlows:HoldSeconds in this
        /// request; wait may ask for less. Answers { runId, status: "running" | "done" | "failed", results,
        /// message, waitSec }: a page that gets "running" keeps asking (GET run/{id}) until waitSec has
        /// passed. Sentences are in the language of the cultureId header.
        /// </summary>
        [HttpPost("run")]
        [RequestSizeLimit(MaxBodyBytes)]
        public async Task<IActionResult> Run([FromBody] PageRunBody? body)
        {
            if (!TryCaller(out var userId, out var siteId, out var problem))
                return problem!;
            var result = await _pageRuns.StartAsync(siteId, userId, HeaderLong("cultureId"), body?.Flow, body?.Input, body?.Wait, HttpContext.RequestAborted);
            return Answer(result);
        }

        /// <summary>
        /// "Read the result" without a runId: the run this visitor's journey (or the visitor, or the
        /// input's value) already has, without starting one; for a flow that runs after a payment, the
        /// run of the order the payment page names (input orderId), for the order's customer only.
        /// Same answer as run; 404 when none exists.
        /// </summary>
        [HttpPost("result")]
        [RequestSizeLimit(MaxBodyBytes)]
        public async Task<IActionResult> Result([FromBody] PageRunBody? body)
        {
            if (!TryCaller(out var userId, out var siteId, out var problem))
                return problem!;
            return Answer(await _pageRuns.FindAsync(siteId, userId, HeaderLong("cultureId"), body?.Flow, body?.Input, body?.Wait, HttpContext.RequestAborted));
        }

        /// <summary>The state and results of a run this visitor may read; ?wait=10 waits up to 10 s (never longer than its flow says).</summary>
        [HttpGet("run/{id:long}")]
        public async Task<IActionResult> GetRun(long id, [FromQuery] int? wait)
        {
            if (!TryCaller(out var userId, out var siteId, out var problem))
                return problem!;
            return Answer(await _pageRuns.GetAsync(siteId, userId, HeaderLong("cultureId"), id, wait, HttpContext.RequestAborted));
        }

        private bool TryCaller(out long userId, out long siteId, out IActionResult? problem)
        {
            problem = null;
            siteId = HeaderLong("siteId");
            // the token's own user, never the userId header (anyone can send that)
            var userClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userClaim, NumberStyles.None, CultureInfo.InvariantCulture, out userId))
            {
                problem = Unauthorized();
                return false;
            }
            if (siteId <= 0)
            {
                problem = BadRequest(new { error = "The siteId header is required." });
                return false;
            }
            return true;
        }

        private long HeaderLong(string name) =>
            long.TryParse(Request.Headers[name].ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0;

        private IActionResult Answer(PageRunResult result) => this.ToResponse(result);
    }
}
