using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace EskaCMS.Middlewares.Middlewares
{
    // Per-request timing/logging middleware. Gives ongoing production visibility into slow
    // requests (the exact "% of slow requests" signal needed to detect CPU overload early,
    // per standard IIS/ASP.NET Core performance-triage practice) WITHOUT adding per-request
    // logging overhead: only requests that cross the configured threshold are logged. Does not
    // buffer or log request/response bodies (cheap: a Stopwatch + one comparison per request).
    //
    // Configure via appsettings:
    //   "RequestTiming": { "SlowRequestThresholdMs": 1000 }
    // (defaults to 1000ms if unset/absent — tune down temporarily while diagnosing a CPU
    // incident, back up once resolved, no redeploy needed since it re-reads IConfiguration
    // per request via IOptionsMonitor-free direct IConfiguration access, which is cheap and
    // already reflects config-reload if the host has reloadOnChange enabled).
    public class RequestTimingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTimingMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                var thresholdMs = _configuration.GetValue<int?>("RequestTiming:SlowRequestThresholdMs") ?? 1000;
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                if (elapsedMs >= thresholdMs)
                {
                    var correlationId = context.Items.TryGetValue("CorrelationId", out var existing) && existing != null
                        ? existing.ToString()
                        : context.Response.Headers.TryGetValue("X-Correlation-ID", out var header)
                            ? header.ToString()
                            : context.TraceIdentifier;

                    Log.SlowRequest(_logger, context.Request.Method, context.Request.Path.Value ?? string.Empty,
                        context.Response.StatusCode, elapsedMs, correlationId);
                }
                else if (_logger.IsEnabled(LogLevel.Debug))
                {
                    // Debug-level, per-request line — disabled by default (near-zero cost via the
                    // IsEnabled gate above), available on demand by lowering the configured log
                    // level without a redeploy, for full request-level diagnosis.
                    Log.RequestCompleted(_logger, context.Request.Method, context.Request.Path.Value ?? string.Empty,
                        context.Response.StatusCode, elapsedMs);
                }
            }
        }

        // Source-generated logging: compiled once, allocation-free when the level is disabled,
        // no string interpolation in the call site (matches project logging conventions).
        private static class Log
        {
            private static readonly Action<ILogger, string, string, int, long, string, Exception> _slowRequest =
                LoggerMessage.Define<string, string, int, long, string>(
                    LogLevel.Warning,
                    new EventId(9001, nameof(SlowRequest)),
                    "Slow request: {Method} {Path} responded {StatusCode} in {ElapsedMs}ms. CorrelationId: {CorrelationId}");

            private static readonly Action<ILogger, string, string, int, long, Exception> _requestCompleted =
                LoggerMessage.Define<string, string, int, long>(
                    LogLevel.Debug,
                    new EventId(9002, nameof(RequestCompleted)),
                    "{Method} {Path} responded {StatusCode} in {ElapsedMs}ms");

            public static void SlowRequest(ILogger logger, string method, string path, int statusCode, long elapsedMs, string correlationId)
                => _slowRequest(logger, method, path, statusCode, elapsedMs, correlationId, null);

            public static void RequestCompleted(ILogger logger, string method, string path, int statusCode, long elapsedMs)
                => _requestCompleted(logger, method, path, statusCode, elapsedMs, null);
        }
    }
}
