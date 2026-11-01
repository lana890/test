using EskaCMS.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace EskaCMS.Middlewares.Middlewares
{
    // The last line of defence for every request: whatever escapes a controller, a filter, a
    // service or another middleware is turned here into one consistent, user-safe JSON body.
    //
    // It replaces the inline app.Use(...) global handler that each host used to declare for
    // itself (WebHost and Engine held a copy of the same lambda), and it answers the error
    // shape defined once in ApiErrorResponse:
    //   { "code": "500", "message": "...", "error": "...", "correlationId": "...", "traceId": "..." }
    // "message" is always a readable sentence - never an object, never null - and never carries
    // a stack trace, a database detail or any other internal information. ExceptionMappingHelper
    // decides the status code, so a cancelled request, a missing record, a conflict, a timeout
    // and an unavailable integration are no longer all reported as 500.
    //
    // Register it as early as possible (right after request timing) so it also covers the
    // middlewares that run after it. MVC pages keep their own error pages: in production
    // UseExceptionHandler("/Home/Error") is registered further in and handles non-API requests
    // before the exception ever reaches here.
    public class GlobalExceptionHandlingMiddleware
    {
        private const string CorrelationIdHeaderName = "X-Correlation-ID";
        private const string CorrelationIdItemKey = "CorrelationId";

        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            // The property names are pinned by attributes on ApiErrorResponse; this only keeps
            // the output compact.
            WriteIndented = false
        };

        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var correlationId = ResolveCorrelationId(context);

            context.Response.Headers[CorrelationIdHeaderName] = correlationId;

            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                var error = ExceptionMappingHelper.Map(exception);

                var clientDisconnected =
                    error.StatusCode == ExceptionMappingHelper.ClientClosedRequestStatusCode
                    && context.RequestAborted.IsCancellationRequested;

                Log(context, exception, error, correlationId, clientDisconnected);

                if (context.Response.HasStarted)
                {
                    // Part of the response is already on the wire: the status code and the body
                    // can no longer be replaced, so let the server abort the response instead of
                    // completing a truncated one that the client would read as a success.
                    throw;
                }

                context.Response.Clear();
                context.Response.Headers[CorrelationIdHeaderName] = correlationId;
                context.Response.StatusCode = error.StatusCode;

                if (clientDisconnected)
                {
                    // Nobody is left to read a body.
                    return;
                }

                var body = ApiErrorResponse.From(error);

                body.CorrelationId = correlationId;
                body.TraceId = context.TraceIdentifier;

                context.Response.ContentType = "application/json; charset=utf-8";

                await context.Response.WriteAsync(JsonSerializer.Serialize(body, SerializerOptions));
            }
        }

        private static string ResolveCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var incomingCorrelationId)
                && !string.IsNullOrWhiteSpace(incomingCorrelationId.ToString()))
            {
                return incomingCorrelationId.ToString();
            }

            if (context.Items.TryGetValue(CorrelationIdItemKey, out var existingCorrelationId)
                && existingCorrelationId != null)
            {
                return existingCorrelationId.ToString() ?? context.TraceIdentifier;
            }

            return context.TraceIdentifier;
        }

        private void Log(
            HttpContext context,
            Exception exception,
            ApiErrorInfo error,
            string correlationId,
            bool clientDisconnected)
        {
            if (clientDisconnected)
            {
                // Routine: the browser navigated away or the caller timed out.
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        "Request cancelled by the client for {Method} {Path}. CorrelationId: {CorrelationId}",
                        context.Request.Method,
                        context.Request.Path.Value ?? string.Empty,
                        correlationId);
                }

                return;
            }

            // Unexpected failures carry the full exception at Error level - that is what makes a
            // production issue debuggable. Recognised failures (validation, business rules, a
            // missing record) are expected traffic and would only add noise at Error level.
            if (error.IsUnexpected || error.StatusCode >= 500)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception answered as {StatusCode} for {Method} {Path}. CorrelationId: {CorrelationId}. TraceId: {TraceId}",
                    error.StatusCode,
                    context.Request.Method,
                    context.Request.Path.Value ?? string.Empty,
                    correlationId,
                    context.TraceIdentifier);
            }
            else
            {
                _logger.LogWarning(
                    exception,
                    "Request failed with {StatusCode} for {Method} {Path}. CorrelationId: {CorrelationId}. TraceId: {TraceId}",
                    error.StatusCode,
                    context.Request.Method,
                    context.Request.Path.Value ?? string.Empty,
                    correlationId,
                    context.TraceIdentifier);
            }
        }
    }
}
