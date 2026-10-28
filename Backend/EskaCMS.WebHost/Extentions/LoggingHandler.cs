using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.EcomLog;
using EskaCommerce.ShoppingCart.EcommerceLog.ModelView;
using EskaCommerce.ShoppingCart.EcommerceLog.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EskaCMS.Middlewares.Handlers
{
    public class LoggingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHandler> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly IReadOnlyList<string> _pathsToLog;

        public LoggingHandler(
            ILogger<LoggingHandler> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _pathsToLog = configuration
                .GetSection("LogPaths")
                .Get<List<string>>()?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray() ?? Array.Empty<string>();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Skip logging flag
            if (request.Headers.Contains("X-Skip-Logging"))
                return await base.SendAsync(request, cancellationToken);

            if (!ShouldLogRequest(request.RequestUri?.AbsolutePath))
                return await base.SendAsync(request, cancellationToken);

            using var scope = _serviceProvider.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<IEcommerceLogService>();

            long siteId = 0;
            long userId = 0;
            long? cartId =null;

            try
            {
                var workContext = scope.ServiceProvider.GetRequiredService<IWorkContext>();
                siteId = workContext.GetCurrentSiteId();
                userId = await workContext.GetCurrentUserId();
                cartId= workContext.GetCartId();

            }
            catch { /* No HttpContext in background jobs — safe fallback */ }

            // Capture request without breaking it
            var requestPayload = await CaptureRequestAsync(request);

            EcommerceLogs? log = null;


            var response = await base.SendAsync(request, cancellationToken);
            try
            {
                var responseBody = await CaptureResponseAsync(response);

                // Parse StatusCode from response body since APIs always return HTTP 200
                var effectiveStatusCode = GetEffectiveStatusCode(responseBody, (int)response.StatusCode);

                if (responseBody != null)
                    log = await logService.CreateLogWithResponseAsync(
                        requestPayload,
                        responseBody,
                        effectiveStatusCode,   // log the real status, not always 200
                        userId,
                        siteId,
                        cartId);

                // If business StatusCode != 1, return a 400 response instead of 200
                if (effectiveStatusCode == 400)
                {
                    var newResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content    = new StringContent(responseBody, Encoding.UTF8, "application/json"),
                        RequestMessage = request
                    };
                    return newResponse;
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EcommerceLoggingHandler for {Url}", request.RequestUri);
                try
                {
                    if (log != null)
                        await logService.UpdateLogWithErrorAsync(log.Id, ex);
                }
                catch { }
                return response;
            }
        }
        private static int GetEffectiveStatusCode(string responseBody, int fallback)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                return fallback;
            try
            {
                var json = JObject.Parse(responseBody);

                var statusCode =
                    json["Status"]?["StatusCode"]?.Value<int>()
                    ?? json["StatusCode"]?.Value<int>();

                // No StatusCode field found → return HTTP status as-is
                if (statusCode == null)
                    return fallback;

                return statusCode == 1 ? 200 : 400;
            }
            catch
            {
                return fallback;
            }
        }
        // ===================== REQUEST CAPTURE =====================

        //private async Task<RequestPayload> CaptureRequestAsync(HttpRequestMessage request)
        //{
        //    string? body = null;

        //    if (request.Content != null)
        //    {
        //        var contentType = request.Content.Headers.ContentType?.MediaType ?? string.Empty;

        //        try
        //        {
        //            if (contentType.Contains("multipart/form-data"))
        //            {
        //                // Buffer so the original content stream can still be sent downstream
        //                await request.Content.LoadIntoBufferAsync();

        //                try
        //                {
        //                    var fields = new Dictionary<string, string>();
        //                    var multipart = await request.Content.ReadAsMultipartAsync();

        //                    foreach (var section in multipart.Contents)
        //                    {
        //                        var disposition = section.Headers.ContentDisposition;
        //                        if (disposition?.Name == null) continue;

        //                        var fieldName = disposition.Name.Trim('"');

        //                        // File part â€” binary, store metadata only
        //                        if (disposition.FileName != null)
        //                        {
        //                            fields[fieldName] = $"[Binary file: {disposition.FileName}]";
        //                        }
        //                        else
        //                        {
        //                            fields[fieldName] = await section.ReadAsStringAsync();
        //                        }
        //                    }

        //                    body = JsonSerializer.Serialize(fields);
        //                }
        //                catch (Exception ex)
        //                {
        //                    body = await request.Content.ReadAsStringAsync();
        //                    _logger.LogWarning("Failed to parse multipart fields: {Error}", ex.Message);
        //                }
        //            }
        //            else if (
        //                contentType.Contains("application/json") ||
        //                contentType.Contains("text/") ||
        //                contentType.Contains("application/x-www-form-urlencoded") ||
        //                contentType.Contains("application/xml"))
        //            {
        //                // Save original headers
        //                var originalHeaders = request.Content.Headers
        //                    .ToDictionary(h => h.Key, h => h.Value.ToList());

        //                body = await request.Content.ReadAsStringAsync();

        //                // Re-create content and restore headers
        //                var bytes = Encoding.UTF8.GetBytes(body);
        //                var newContent = new ByteArrayContent(bytes);

        //                foreach (var header in originalHeaders)
        //                    newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);

        //                request.Content = newContent;
        //            }
        //            else
        //            {
        //                // Binary / unknown â€” just note content type, don't read bytes
        //                body = $"[Binary content: {contentType}, Length: {request.Content.Headers.ContentLength ?? -1} bytes]";
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            body = $"[Failed to read request body: {ex.Message}]";
        //        }
        //    }

        //    var headers = request.Headers
        //        .ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

        //    // Add content headers too
        //    if (request.Content != null)
        //    {
        //        foreach (var h in request.Content.Headers)
        //            headers[h.Key] = string.Join(", ", h.Value);
        //    }

        //    return new RequestPayload
        //    {
        //        Method = request.Method.Method,
        //        Url = request.RequestUri?.ToString(),
        //        Body = body,
        //        Headers = headers
        //    };
        //}

        private async Task<string> CaptureResponseAsync(HttpResponseMessage response)
        {
            if (response.Content == null)
                return string.Empty;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

            try
            {
                if (
                    contentType.Contains("application/json") ||
                    contentType.Contains("text/") ||
                    contentType.Contains("application/xml"))
                {
                    var originalHeaders = response.Content.Headers
                        .ToDictionary(h => h.Key, h => h.Value.ToList());

                    var responseBody = await response.Content.ReadAsStringAsync();

                    var bytes = Encoding.UTF8.GetBytes(responseBody);
                    var newContent = new ByteArrayContent(bytes);

                    foreach (var header in originalHeaders)
                        newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);

                    response.Content = newContent;

                    return responseBody;
                }
                else if (contentType.Contains("multipart"))
                {
                    await response.Content.LoadIntoBufferAsync();
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    // Binary response — just log metadata
                    return $"[Binary response: {contentType}, Length: {response.Content.Headers.ContentLength ?? -1} bytes]";
                }
            }
            catch (Exception ex)
            {
                return $"[Failed to read response body: {ex.Message}]";
            }
        }


        private bool ShouldLogRequest(string? path)
        {
            if (string.IsNullOrEmpty(path) || _pathsToLog.Count == 0)
                return false;

            return _pathsToLog.Any(p =>
                path.Contains(p, StringComparison.OrdinalIgnoreCase));
        }
        private async Task<RequestPayload> CaptureRequestAsync(HttpRequestMessage request)
        {
            string? body = null;

            if (request.Content != null)
            {
                var contentType = request.Content.Headers.ContentType?.MediaType ?? string.Empty;

                try
                {
                    if (contentType.Contains("multipart/form-data"))
                    {
                        await request.Content.LoadIntoBufferAsync();
                        var rawBody = await request.Content.ReadAsStringAsync();

                        // Extract boundary from Content-Type
                        var boundary = request.Content.Headers.ContentType?.Parameters
                            .FirstOrDefault(p => p.Name.Equals("boundary", StringComparison.OrdinalIgnoreCase))
                            ?.Value?.Trim('"');

                        body = TryParseMultipartToJson(rawBody, boundary);
                    }
                    else if (
                        contentType.Contains("application/json") ||
                        contentType.Contains("text/") ||
                        contentType.Contains("application/x-www-form-urlencoded") ||
                        contentType.Contains("application/xml"))
                    {
                        var originalHeaders = request.Content.Headers
                            .ToDictionary(h => h.Key, h => h.Value.ToList());

                        body = await request.Content.ReadAsStringAsync();

                        // Rebuild content so it can still be sent downstream
                        var newContent = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
                        foreach (var header in originalHeaders)
                            newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);

                        request.Content = newContent;
                    }
                    else
                    {
                        body = $"[Binary content: {contentType}, Length: {request.Content.Headers.ContentLength ?? -1} bytes]";
                    }
                }
                catch (Exception ex)
                {
                    body = $"[Failed to read request body: {ex.Message}]";
                }
            }

            var headers = request.Headers
                .ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

            if (request.Content != null)
                foreach (var h in request.Content.Headers)
                    headers[h.Key] = string.Join(", ", h.Value);

            return new RequestPayload
            {
                Method = request.Method.Method,
                Url = request.RequestUri?.ToString(),
                Body = body,
                Headers = headers
            };
        }
        private string TryParseMultipartToJson(string rawBody, string? boundary)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(boundary))
                    return rawBody;

                var fields = new Dictionary<string, string>();

                var parts = rawBody.Split(
                    new[] { $"--{boundary}" },
                    StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    if (part.StartsWith("--")) continue;

                    var sections = part.Split(
                        new[] { "\r\n\r\n" },
                        StringSplitOptions.None);

                    if (sections.Length < 2) continue;

                    var headers = sections[0];
                    var value = sections[1].Trim('\r', '\n');

                    var nameMatch = Regex.Match(headers, @"name=""?([^"";\r\n]+)""?");
                    if (!nameMatch.Success) continue;

                    fields[nameMatch.Groups[1].Value] = value;
                }

                return fields.Any()
                    ? JsonSerializer.Serialize(fields)
                    : rawBody;
            }
            catch
            {
                return rawBody;
            }
        }
    }



	public class ApiLoggingMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<ApiLoggingMiddleware> _logger;
		private readonly IConfiguration _configuration;

		public ApiLoggingMiddleware(
			RequestDelegate next,
			ILogger<ApiLoggingMiddleware> logger,
			IConfiguration configuration)
		{
			_next = next;
			_logger = logger;
			_configuration = configuration;
		}

		public async Task InvokeAsync(
			HttpContext context,
			IEcommerceLogService logService,
			IWorkContext workContext)
		{
			// Skip logging flag
			if (context.Request.Headers.ContainsKey("X-Skip-Logging"))
			{
				await _next(context);
				return;
			}

			if (!ShouldLogRequest(context.Request.Path))
			{
				await _next(context);
				return;
			}

			long siteId = 0;
			long userId = 0;
            long? cartId = null;
			try
			{
				siteId = workContext.GetCurrentSiteId();
				cartId =  workContext.GetCartId();

				userId = await workContext.GetCurrentUserId();

			}
			catch { /* No HttpContext in background jobs — safe fallback */ }

			// Capture Request
			var requestPayload = await CaptureRequestAsync(context.Request);

			// Buffer the Response
			var originalResponseBody = context.Response.Body;
			using var responseBuffer = new MemoryStream();
			context.Response.Body = responseBuffer;

			EcommerceLogs? log = null;

			try
			{
				await _next(context);

				// Capture Response
				responseBuffer.Seek(0, SeekOrigin.Begin);
				var responseBody = await new StreamReader(responseBuffer).ReadToEndAsync();

				// Write response back to original stream
				responseBuffer.Seek(0, SeekOrigin.Begin);
				await responseBuffer.CopyToAsync(originalResponseBody);
				context.Response.Body = originalResponseBody;

				var effectiveStatusCode = GetEffectiveStatusCode(responseBody, context.Response.StatusCode);

				if (!string.IsNullOrWhiteSpace(responseBody))
					log = await logService.CreateLogWithResponseAsync(
						requestPayload,
						responseBody,
						effectiveStatusCode,
						userId,
						siteId,
                        cartId);

				// Override response to 400 if business status failed
				if (effectiveStatusCode == 400 && !context.Response.HasStarted)
				{
					context.Response.StatusCode = 400;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ApiLoggingMiddleware for {Path}", context.Request.Path);
				context.Response.Body = originalResponseBody;

				try
				{
					if (log != null)
						await logService.UpdateLogWithErrorAsync(log.Id, ex);
				}
				catch { }
			}
		}

		// ===================== REQUEST CAPTURE =====================

		private async Task<RequestPayload> CaptureRequestAsync(HttpRequest request)
		{
			string? body = null;
			request.EnableBuffering();

			var contentType = request.ContentType ?? string.Empty;

			try
			{
				if (contentType.Contains("multipart/form-data"))
				{
					var boundary = contentType.Split(';')
						.FirstOrDefault(p => p.Trim().StartsWith("boundary="))
						?.Split('=').LastOrDefault()?.Trim('"');

					var rawBody = await new StreamReader(request.Body, leaveOpen: true).ReadToEndAsync();
					request.Body.Position = 0;

					body = TryParseMultipartToJson(rawBody, boundary);
				}
				else if (
					contentType.Contains("application/json") ||
					contentType.Contains("text/") ||
					contentType.Contains("application/x-www-form-urlencoded") ||
					contentType.Contains("application/xml"))
				{
					body = await new StreamReader(request.Body, leaveOpen: true).ReadToEndAsync();
					request.Body.Position = 0; // Reset so controller can read it
				}
				else if (!string.IsNullOrEmpty(contentType))
				{
					body = $"[Binary content: {contentType}, Length: {request.ContentLength ?? -1} bytes]";
				}
			}
			catch (Exception ex)
			{
				body = $"[Failed to read request body: {ex.Message}]";
			}

			var headers = request.Headers
				.ToDictionary(h => h.Key, h => string.Join(", ", h.Value.ToArray()));

			return new RequestPayload
			{
				Method = request.Method,
				Url = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}",
				Body = body,
				Headers = headers
			};
		}

		// ===================== RESPONSE CAPTURE =====================

		private async Task<string> CaptureResponseAsync(HttpResponse response, MemoryStream buffer)
		{
			var contentType = response.ContentType ?? string.Empty;

			try
			{
				buffer.Seek(0, SeekOrigin.Begin);

				if (contentType.Contains("application/json") ||
					contentType.Contains("text/") ||
					contentType.Contains("application/xml"))
				{
					return await new StreamReader(buffer, leaveOpen: true).ReadToEndAsync();
				}
				else if (contentType.Contains("multipart"))
				{
					return await new StreamReader(buffer, leaveOpen: true).ReadToEndAsync();
				}
				else
				{
					return $"[Binary response: {contentType}, Length: {response.ContentLength ?? -1} bytes]";
				}
			}
			catch (Exception ex)
			{
				return $"[Failed to read response body: {ex.Message}]";
			}
		}

		// ===================== HELPERS =====================

		private static int GetEffectiveStatusCode(string responseBody, int fallback)
		{
			if (string.IsNullOrWhiteSpace(responseBody))
				return fallback;
			try
			{
				var json = JObject.Parse(responseBody);
				var statusCode =
					json["Status"]?["StatusCode"]?.Value<int>()
					?? json["StatusCode"]?.Value<int>();

				if (statusCode == null) return fallback;

				return statusCode == 1 ? 200 : 400;
			}
			catch
			{
				return fallback;
			}
		}

		private bool ShouldLogRequest(PathString path)
		{
			try
			{
				if (!path.HasValue) return false;

				var pathsToLog = _configuration
					.GetSection("LogPaths")
					.Get<List<string>>();

				if (pathsToLog != null && pathsToLog.Count > 0)
					return pathsToLog.Any(p =>
						path.Value!.Contains(p, StringComparison.OrdinalIgnoreCase));

				return false;
			}
			catch
			{
				return false;
			}
		}

		private string TryParseMultipartToJson(string rawBody, string? boundary)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(boundary)) return rawBody;

				var fields = new Dictionary<string, string>();
				var parts = rawBody.Split(
					new[] { $"--{boundary}" },
					StringSplitOptions.RemoveEmptyEntries);

				foreach (var part in parts)
				{
					if (part.StartsWith("--")) continue;

					var sections = part.Split(
						new[] { "\r\n\r\n" },
						StringSplitOptions.None);

					if (sections.Length < 2) continue;

					var headers = sections[0];
					var value = sections[1].Trim('\r', '\n');

					var nameMatch = Regex.Match(headers, @"name=""?([^"";\r\n]+)""?");
					if (!nameMatch.Success) continue;

					fields[nameMatch.Groups[1].Value] = value;
				}

				return fields.Any() ? JsonSerializer.Serialize(fields) : rawBody;
			}
			catch
			{
				return rawBody;
			}
		}
	}
}
