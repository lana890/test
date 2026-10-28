using EskaCMS.Core.Cache;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Filters.Helpers;
using EskaCMS.Filters.Helpers.Interfaces;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Middlewares.ViewModels;
using Logger_Nuget.Helpers.Interfaces;
using Logger_Nuget.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EskaCMS.Middlewares.Middlewares
{
    public class ApiWrapper
    {
        private readonly List<string> SecuredApisList;
        private readonly List<string> ChangeEmptyResponseAPIs;
        private readonly bool ApiLogStatus;

        private readonly List<string> ExecludedApis;
        private readonly string IntegrationPermission;

        private readonly string ApiVersion;
        private readonly string ApiName;

        private readonly RequestDelegate Next;

        private readonly IHashingHelper _hashingHelper;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly int EXPIRATION_PRIOD_IN_DAYS;
        private readonly IConfiguration _Configration;
        private readonly bool IsCypherActive;

        private readonly List<string> ExecludedHosts;
        private readonly List<string> ApiWrapperExecludedApis;
        private readonly bool ApiLoggerEnabled;

        // Cache key prefix for the per-pointer CypherParam lookup in SecureDataWrapAsync.
        // The pointer is appended so every record gets its own entry (CypherParam:{pointer}).
        private const string CypherParamCacheKeyPrefix = "CypherParam:";

        // Falls back to 10 minutes, matching the short fixed TTL already used for cached
        // read-mostly data elsewhere (see ProductWebServices: AddMinutes(10)).
        // CacheConfig:CachePeriod is deliberately NOT reused here: it is "0" in appsettings.json,
        // which would expire the entry immediately and make the cache a no-op.
        private const int DefaultCypherParamCacheMinutes = 10;
        private readonly int CypherParamCacheMinutes;

        private static readonly int ApiLoggerMaxConcurrency = Math.Max(2, Environment.ProcessorCount);
        private static readonly SemaphoreSlim ApiLoggerGate = new(ApiLoggerMaxConcurrency, ApiLoggerMaxConcurrency);

        // Reuse ONE serializer settings + snake_case resolver across all requests so Newtonsoft's
        // per-resolver contract cache is reused (was: a new resolver per response, which forced
        // reflection-based contract building on every API call). Settings are request-independent.
        private static readonly JsonSerializerSettings WrapperSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy
                {
                    ProcessDictionaryKeys = true
                }
            }
        };

        public ApiWrapper(
            string version,
            string name,
            RequestDelegate requestDelegate,
            IHashingHelper hashingHelper,
            IServiceScopeFactory serviceScopeFactory,
            IConfiguration Configration)
        {
            ApiVersion = version;
            ApiName = name;
            Next = requestDelegate;
            _hashingHelper = hashingHelper;
            _serviceScopeFactory = serviceScopeFactory;
            _Configration = Configration;

            IntegrationPermission = _Configration.GetSection("CypherSecurity:IntegrationPermission").Get<string>() ?? string.Empty;
            ExecludedApis = _Configration.GetSection("CypherSecurity:ExecludedApis").Get<List<string>>() ?? new List<string>();
            SecuredApisList = _Configration.GetSection("CypherSecurity:SecuredApisList").Get<List<string>>() ?? new List<string>();
            ExecludedHosts = _Configration.GetSection("CypherSecurity:ExecludedHosts").Get<List<string>>() ?? new List<string>();
            ApiWrapperExecludedApis = _Configration.GetSection("CypherSecurity:ApiWrapperExecludedApis").Get<List<string>>() ?? new List<string>();
            ChangeEmptyResponseAPIs = _Configration.GetSection("Wrapper:ChangeEmptyResponseAPIs").Get<List<string>>() ?? new List<string>();

            int.TryParse(_Configration.GetSection("CypherSecurity:EXPIRATION_PERIOD_IN_DAYS").Value, out EXPIRATION_PRIOD_IN_DAYS);
            bool.TryParse(_Configration.GetSection("CypherSecurity:IS_ACTIVE").Value, out IsCypherActive);
            bool.TryParse(_Configration.GetSection("ApiLog:ApiLogStatus:IsActive").Value, out ApiLogStatus);
            bool.TryParse(_Configration.GetSection("ApiLogger:Enabled").Value, out ApiLoggerEnabled);

            // Optional override; unset or non-positive values keep the 10 minute default.
            if (!int.TryParse(_Configration.GetSection("CypherSecurity:CypherParamCacheMinutes").Value, out CypherParamCacheMinutes)
                || CypherParamCacheMinutes <= 0)
            {
                CypherParamCacheMinutes = DefaultCypherParamCacheMinutes;
            }
        }

        public async Task Invoke(HttpContext context, IApiLogService IApiLogService, IWorkContext workContext)
        {
            var stopwatch = Stopwatch.StartNew();

            if (IsCypherActive)
            {
                context.Request.EnableBuffering();

                var canContinue = await PrepareRequest(context);

                if (!canContinue)
                    return;
            }

            if (IsExecludedApiFromWrapper(context.Request))
            {
                await Next(context);
                return;
            }

            var needsRequestBody = ApiLogStatus || ApiLoggerEnabled;
            var requestBody = string.Empty;

            if (needsRequestBody)
            {
                context.Request.EnableBuffering();
                requestBody = await ReadRequestBodyAsync(context.Request);
            }

            ApiLogVM logobj = null;

            if (ApiLogStatus)
            {
                var siteid = workContext.GetCurrentSiteId();
                var userid = await workContext.GetCurrentUserId();
                var request = FormatRequest(context.Request, requestBody);

                logobj = new ApiLogVM
                {
                    ApiUrl = context.Request.Path.Value,
                    FullUrl = context.Request.Host.Value,
                    Request = request,
                    HttpMethod = context.Request.Method,
                    SiteId = siteid == 0 ? 1 : siteid,
                    UserId = userid == 0 ? 1 : userid
                };
            }

            var originalResponseBody = context.Response.Body;

            await using var memoryStream = new MemoryStream();

            try
            {
                context.Response.Body = memoryStream;

                await Next(context);

                stopwatch.Stop();

                memoryStream.Position = 0;

                string jsonString;

                using (var reader = new StreamReader(
                    memoryStream,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true))
                {
                    jsonString = await reader.ReadToEndAsync();
                }

                if (ApiLogStatus && logobj != null)
                {
                    logobj.Response = jsonString;
                    logobj.StatusCode = context.Response.StatusCode;
                }

                if (ApiLoggerEnabled && TryCreateOrderRequest(context, requestBody, stopwatch.ElapsedMilliseconds, out var createApiLogRequest))
                {
                    createApiLogRequest.ResponsePayload = jsonString;

                    _ = TryCreateOrderLogAsync(createApiLogRequest);
                }

                if (ApiLogStatus && logobj != null)
                {
                    await IApiLogService.CreateLog(logobj);
                }

                if (context.Response.HasStarted)
                {
                    context.Response.Body = originalResponseBody;
                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(originalResponseBody);
                    return;
                }

                var wrappedResponse = await WrapAsync(jsonString, context);

                context.Response.Body = originalResponseBody;

                context.Response.ContentType = "application/vnd.api+json";
                context.Response.Headers.Remove("Content-Length");

                // Perf #02: write the string directly instead of allocating an intermediate byte[].
                // WriteAsync encodes as UTF-8 through pooled buffers, so the bytes on the wire are
                // identical - it just removes one full-size allocation per response, which for
                // payloads over ~85KB was landing on the Large Object Heap.
                await context.Response.WriteAsync(wrappedResponse);
            }
            finally
            {
                context.Response.Body = originalResponseBody;
            }
        }

        public async Task CreateOrderAsync(CreateApiLogRequest request)
        {
            try
            {
                if (!ApiLoggerEnabled)
                    return;

                var apiLogSection = _Configration.GetSection("ApiLogger");

                if (!apiLogSection.Exists())
                    return;

                using var scope = _serviceScopeFactory.CreateScope();

                var logger = scope.ServiceProvider.GetService<IApiLoggerClient>();

                if (logger != null)
                {
                    await logger.CreateApiLogAsync(request);
                }
            }
            catch
            {
                // Do not break the main API response because logging failed.
            }
        }

        private async Task TryCreateOrderLogAsync(CreateApiLogRequest request)
        {
            if (!ApiLoggerGate.Wait(0))
                return;

            try
            {
                await CreateOrderAsync(request);
            }
            finally
            {
                ApiLoggerGate.Release();
            }
        }

        private bool TryCreateOrderRequest(
            HttpContext context,
            string requestBody,
            long durationMs,
            out CreateApiLogRequest obj)
        {
            obj = null;

            // Only calls of a visitor's journey are logged: the logger files every call under
            // its correlation id, and one without (a server render, the Admin) can never be shown.
            if (string.IsNullOrWhiteSpace(context.Request.Headers["correlation_id"].ToString()))
                return false;

            try
            {
                var query = context.Request.Query
                    .ToDictionary(q => q.Key, q => q.Value.ToString());

                var requestHeaders = context.Request.Headers
                    .ToDictionary(h => h.Key, h => h.Value.ToString());

                var responseHeaders = context.Response.Headers
                    .ToDictionary(h => h.Key, h => h.Value.ToString());

                string GetHeader(string key)
                {
                    return context.Request.Headers.TryGetValue(key, out var value)
                        ? value.ToString()
                        : string.Empty;
                }

                // The first of the headers that has a value, cut to the logger's column size.
                // The website sends these URL-encoded and the Admin decodes them, so they go on
                // encoded: the logger client sends them again as headers, and an Arabic step
                // name is not a valid header value.
                string FirstHeader(int maxLength, params string[] keys)
                {
                    foreach (var key in keys)
                    {
                        var value = GetHeader(key).Trim();
                        if (value.Length == 0)
                            continue;
                        if (value.Length <= maxLength && value.All(c => c < 128))
                            return value;

                        // Too long, or not encoded: cut whole characters and encode them, so the
                        // value still decodes (a cut inside an encoded character would not).
                        var text = Uri.UnescapeDataString(value);
                        for (var cut = Math.Min(text.Length, maxLength); cut > 0; cut--)
                        {
                            if (char.IsHighSurrogate(text[cut - 1]))
                                continue;
                            var encoded = Uri.EscapeDataString(text.Substring(0, cut));
                            if (encoded.Length <= maxLength)
                                return encoded;
                        }
                    }

                    return string.Empty;
                }

                // The logger requires a step and a note: a call outside a step says so, and
                // the note is the page the call came from.
                var currentStep = FirstHeader(100, "current_step");
                var note = FirstHeader(1024, "x-page-url");

                obj = new CreateApiLogRequest
                {
                    Endpoint = context.Request.Path.Value,
                    Method = context.Request.Method,
                    VersionId = 1,

                    ProductName = FirstHeader(255, "product_name", "journey_name"),

                    QueryParams = System.Text.Json.JsonSerializer.Serialize(query),
                    RequestHeaders = System.Text.Json.JsonSerializer.Serialize(requestHeaders),
                    RequestPayload = requestBody,

                    StatusCode = context.Response.StatusCode,
                    ResponseHeaders = System.Text.Json.JsonSerializer.Serialize(responseHeaders),
                    ResponsePayload = string.Empty,

                    DurationMs = Convert.ToInt32(durationMs),
                    CurrentStep = currentStep.Length > 0 ? currentStep : "No step",

                    StepOrder = int.TryParse(GetHeader("step_order"), out var stepOrder)
                        ? stepOrder
                        : 0,

                    Note = note.Length > 0 ? note : "DCMS API",

                    JourneyType = GetHeader("journey_type"),
                    JourneyName = GetHeader("journey_name"),
                    CorrelationId = GetHeader("correlation_id"),

                    TotalSteps = int.TryParse(GetHeader("total_steps"), out var totalSteps)
                        ? totalSteps
                        : 0,

                    ProductNameHeader = GetHeader("product_name")
                };

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string FormatRequest(HttpRequest request, string requestBody)
        {
            return $"{request.Scheme} {request.Host}{request.Path} {request.QueryString} {requestBody}";
        }

        private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            if (request.Body == null || !request.Body.CanRead)
                return string.Empty;

            if (HttpMethods.IsGet(request.Method) ||
                HttpMethods.IsHead(request.Method) ||
                request.ContentLength == 0)
            {
                return string.Empty;
            }

            if (request.HttpContext.RequestAborted.IsCancellationRequested)
                return "[Request aborted by client]";

            try
            {
                request.EnableBuffering();

                if (request.Body.CanSeek)
                    request.Body.Position = 0;

                using var reader = new StreamReader(
                    request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true);

                var requestBody = await reader.ReadToEndAsync(request.HttpContext.RequestAborted);

                return requestBody;
            }
            catch (OperationCanceledException) when (request.HttpContext.RequestAborted.IsCancellationRequested)
            {
                return "[Request aborted by client]";
            }
            catch (Microsoft.AspNetCore.Connections.ConnectionResetException)
            {
                return "[Connection reset by remote host]";
            }
            catch (IOException)
            {
                return "[Failed to read request body]";
            }
            catch (ObjectDisposedException)
            {
                return "[Request body stream disposed]";
            }
            finally
            {
                try
                {
                    if (request.Body != null && request.Body.CanSeek)
                        request.Body.Position = 0;
                }
                catch
                {
                    // Ignore reset-position errors.
                }
            }
        }

        private async Task<string> WrapAsync(string originalBody, HttpContext context)
        {
            dynamic response;

            var trimmedBody = originalBody?.Trim() ?? string.Empty;

            // ---- FAST PATH (perf) -------------------------------------------------------------
            // The overwhelmingly common response: a successful (2xx) request returning a JSON
            // object/array, NOT going through the cypher/secure (signed) pipeline. Normally the
            // wrapper parses the body into a JObject/JArray and re-serializes {code,message,content}
            // with the snake_case resolver. That resolver does NOT rewrite JToken content keys
            // (verified by tests in tests/PerfRefactorTests/ApiWrapperSerializationTests.cs) and the
            // body is already compact JSON, so the wrapped output is byte-identical to simply writing
            // the envelope around the body. We therefore skip the whole parse + re-serialize pass
            // (the dominant per-request CPU cost shown by production profiling) and emit it directly.
            // Cypher/secured, empty/no-content, non-JSON, and error responses fall through unchanged.
            if (IsSuccessResponse(context.Response.StatusCode)
                && trimmedBody.Length > 1
                && ((trimmedBody[0] == '{' && trimmedBody[trimmedBody.Length - 1] == '}')
                    || (trimmedBody[0] == '[' && trimmedBody[trimmedBody.Length - 1] == ']')))
            {
                var integrationPermission = context.Request.Headers["integrationPermission"].ToString();
                bool isSecurePath = IsCypherActive
                    && integrationPermission != IntegrationPermission
                    && !IsExecludedApi(context.Request)
                    && !IsExecludedHost(context.Request);

                if (!isSecurePath)
                {
                    return "{\"code\":\"200\",\"message\":\"Ok\",\"content\":" + trimmedBody + "}";
                }
            }
            // ---- end fast path ----------------------------------------------------------------

            if (trimmedBody.StartsWith("{") && trimmedBody.EndsWith("}"))
            {
                response = JObject.Parse(trimmedBody);
            }
            else if (trimmedBody.StartsWith("[") && trimmedBody.EndsWith("]"))
            {
                response = JArray.Parse(trimmedBody);
            }
            else if (string.IsNullOrEmpty(trimmedBody) && context.Response.StatusCode.ToString().StartsWith("2"))
            {
                if (!IsExcludeChangeResponseApi(context.Request))
                {
                    context.Response.StatusCode = 200;
                    response = "No Content Retrieved";
                }
                else
                {
                    context.Response.StatusCode = context.Response.StatusCode == 204 ? 200 : context.Response.StatusCode;
                    response = trimmedBody;
                }
            }
            else
            {
                response = trimmedBody;
            }

            object wrapper;

            if (IsSuccessResponse(context.Response.StatusCode))
            {
                var clientIntegrationPermission = context.Request.Headers["integrationPermission"].ToString();

                if (IsCypherActive &&
                    clientIntegrationPermission != IntegrationPermission &&
                    !IsExecludedApi(context.Request) &&
                    !IsExecludedHost(context.Request))
                {
                    wrapper = await SecureDataWrapAsync(response, context);
                }
                else
                {
                    wrapper = DataWrap(response);
                }
            }
            else
            {
                wrapper = ErrorWrap(response?.ToString(), context, _Configration);
            }

            // Uses the cached static settings (compact JSON + snake_case) — same output,
            // but reuses the contract cache instead of rebuilding it on every response.
            return JsonConvert.SerializeObject(wrapper, WrapperSerializerSettings);
        }

        private bool IsExcludeChangeResponseApi(HttpRequest request)
        {
            if (ChangeEmptyResponseAPIs == null || ChangeEmptyResponseAPIs.Count == 0)
                return false;

            var apiUrl = request.Path.Value ?? string.Empty;

            return ChangeEmptyResponseAPIs.Any(a => apiUrl.Contains(a, StringComparison.OrdinalIgnoreCase));
        }

        private object DataWrap(dynamic response)
        {
            return new
            {
                code = "200",
                message = "Ok",
                content = response
            };
        }

        private async Task<object> SecureDataWrapAsync(dynamic response, HttpContext context)
        {
            var stringResponse = JsonConvert.SerializeObject(response);

            using var scope = _serviceScopeFactory.CreateScope();

            try
            {
                var cypherParamRepo = scope.ServiceProvider
                    .GetRequiredService<IRepositoryWithTypedId<CypherParam, string>>();

                var pointer = context.Request.Headers["pointerId"].ToString();

                // A CypherParam row never changes after it is written (UserService.HandShake only
                // inserts new rows, CypherParamsBackgroundService only deletes expired ones), so the
                // same pointer always resolves to the same record. Serve it from the shared
                // ICacheService instead of re-querying on every wrapped response.
                // Only non-null results are cached: an unknown or empty pointer still reaches the
                // database and still returns "Invalid cypher pointer." exactly as before.
                var cacheService = scope.ServiceProvider.GetService<ICacheService>();
                var cacheKey = CypherParamCacheKeyPrefix + pointer;

                var cypherParam = cacheService?.GetData<CypherParam>(cacheKey);

                if (cypherParam == null)
                {
                    cypherParam = await cypherParamRepo
                        .Query()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == pointer);

                    if (cypherParam != null)
                    {
                        cacheService?.SetData(
                            cacheKey,
                            cypherParam,
                            DateTimeOffset.UtcNow.AddMinutes(CypherParamCacheMinutes));
                    }
                }

                if (cypherParam == null)
                {
                    return new
                    {
                        code = "400.100",
                        message = "Invalid cypher pointer.",
                        content = "Invalid cypher pointer."
                    };
                }

                var iv = new AES_Helper().GenerateRandomIV();

                if (IsSecuredApi(context.Request) && !context.Request.Path.ToString().Contains("HandShake", StringComparison.OrdinalIgnoreCase))
                {
                    var encryptedBody = EncryptRequestBody(cypherParam.MajorKey, iv, stringResponse);
                    encryptedBody = JsonConvert.SerializeObject(encryptedBody);

                    return new
                    {
                        code = "200",
                        message = "Ok",
                        content = encryptedBody,
                        signature = _hashingHelper.GetHash(stringResponse, cypherParam.AuthId),
                        majorSalt = iv
                    };
                }

                return new
                {
                    code = "200",
                    message = "Ok",
                    content = stringResponse,
                    signature = _hashingHelper.GetHash(stringResponse, cypherParam.AuthId)
                };
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.ThrowDeepException(ex);

                return new
                {
                    code = "400.100",
                    message = "Can't wrap the response because of " + ex.Message,
                    content = ex.Message
                };
            }
        }

        private bool IsSecuredApi(HttpRequest request)
        {
            if (SecuredApisList == null || SecuredApisList.Count == 0)
                return false;

            var apiUrl = request.Path.Value ?? string.Empty;

            return SecuredApisList.Any(a => apiUrl.Contains(a, StringComparison.OrdinalIgnoreCase));
        }

        private string EncryptRequestBody(string key, string iv, string body)
        {
            var aesHelper = new AES_Helper();
            var encryptList = aesHelper.Encrypt(key, iv, body);
            return JsonConvert.SerializeObject(encryptList);
        }

        private object ErrorWrap(string response, HttpContext context, IConfiguration configration)
        {
            var reason = string.IsNullOrWhiteSpace(response)
                ? GetStatusCodeMessage(context.Response.StatusCode)
                : response;

            var customCode = string.Empty;
            var customMessage = string.Empty;

            try
            {
                var getError = response?.Split('$');

                if (getError != null && getError.Length > 1)
                {
                    customCode = getError[0];
                    customMessage = getError[1];
                }

                if (!string.IsNullOrWhiteSpace(response) && response.Contains("(401) Unauthorized"))
                {
                    customCode = "401";
                }

                var securityEnabled = configration.GetValue<bool>("SecuirtyPenetration");

                if (securityEnabled)
                {
                    customCode = context.Response.StatusCode.ToString();
                    customMessage = GetStatusCodeMessage(context.Response.StatusCode);
                }
            }
            catch
            {
                // Ignore wrapping parsing errors.
            }

            return new
            {
                code = string.IsNullOrEmpty(customCode)
                    ? context.Response.StatusCode.ToString()
                    : customCode,

                message = string.IsNullOrEmpty(customMessage)
                    ? reason
                    : customMessage
            };
        }

        private bool IsSuccessResponse(int statusCode)
        {
            return statusCode >= 200 && statusCode < 299;
        }

        private bool IsExecludedApi(HttpRequest request)
        {
            if (ExecludedApis == null || ExecludedApis.Count == 0)
                return false;

            var apiUrl = request.Path.Value ?? string.Empty;

            return ExecludedApis.Any(a => apiUrl.Contains(a, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsExecludedHost(HttpRequest request)
        {
            if (ExecludedHosts == null || ExecludedHosts.Count == 0)
                return false;

            var host = request.Headers["Referer"].ToString();

            if (string.IsNullOrWhiteSpace(host))
                return false;

            return ExecludedHosts.Any(a => host.Contains(a, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsExecludedApiFromWrapper(HttpRequest request)
        {
            if (ApiWrapperExecludedApis == null || ApiWrapperExecludedApis.Count == 0)
                return false;

            var apiUrl = request.Path.Value ?? string.Empty;

            return ApiWrapperExecludedApis.Any(a => apiUrl.Contains(a, StringComparison.OrdinalIgnoreCase));
        }

        private string GetStatusCodeMessage(int statusCode)
        {
            return statusCode switch
            {
                400 => "Bad request.",
                401 => "Unauthorized access.",
                402 => "Payment required.",
                403 => "Forbidden access.",
                404 => "Resource not found.",
                405 => "Method not allowed.",
                406 => "Not acceptable.",
                407 => "Proxy authentication required.",
                408 => "Request timeout.",
                409 => "Conflict.",
                410 => "Resource is gone.",
                411 => "Length is required.",
                415 => "Unsupported media type.",
                422 => "The request contains validation errors.",
                429 => "Too many requests. Please try again later.",

                500 => "Internal server error.",
                501 => "Not implemented.",
                502 => "Bad gateway.",
                503 => "Service unavailable.",
                504 => "Gateway timeout.",
                505 => "HTTP version not supported.",

                _ => statusCode >= 400 && statusCode < 500
                    ? "A client error occurred."
                    : statusCode >= 500
                        ? "A server error occurred."
                        : "Unknown status code."
            };
        }

        private async Task<string> GenerateHash(string text)
        {
            var sb = new StringBuilder();

            using SHA256 hash = SHA256.Create();

            var result = hash.ComputeHash(Encoding.UTF8.GetBytes(text));

            foreach (var b in result)
                sb.Append(b.ToString("x2"));

            return await Task.FromResult(sb.ToString());
        }

        private async Task<CypherParam> GetCypherParam(string pointerId)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var cypherParamRepo = scope.ServiceProvider
                .GetRequiredService<IRepositoryWithTypedId<CypherParam, string>>();

            // Shares the cache entry with SecureDataWrapAsync: same lookup, same key, so the two
            // cypher paths in one request cost a single database read.
            // Caching cannot widen the window in which a pointer is accepted: rows are immutable,
            // the only deletion is CypherParamsBackgroundService removing rows past
            // CypherSecurity:EXPIRATION_PERIOD_IN_DAYS, and the caller re-applies that same
            // expiry check to whatever this returns. Only non-null results are cached, so an
            // unknown pointer still reaches the database and still fails validation as before.
            var cacheService = scope.ServiceProvider.GetService<ICacheService>();
            var cacheKey = CypherParamCacheKeyPrefix + pointerId;

            var cypherParam = cacheService?.GetData<CypherParam>(cacheKey);

            if (cypherParam == null)
            {
                cypherParam = await cypherParamRepo
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == pointerId);

                if (cypherParam != null)
                {
                    cacheService?.SetData(
                        cacheKey,
                        cypherParam,
                        DateTimeOffset.UtcNow.AddMinutes(CypherParamCacheMinutes));
                }
            }

            return cypherParam;
        }

        public async Task<string> GetRawBodyStringAsync(HttpRequest request, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;

            return await ReadBodySafelyAsync(request, encoding);
        }

        private async Task<string> FormatRequest(HttpRequest request, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;

            return await ReadBodySafelyAsync(request, encoding);
        }

        private static async Task<string> ReadBodySafelyAsync(HttpRequest request, Encoding encoding)
        {
            if (request.Body == null || !request.Body.CanRead)
                return string.Empty;

            if (HttpMethods.IsGet(request.Method) ||
                HttpMethods.IsHead(request.Method) ||
                request.ContentLength == 0)
            {
                return string.Empty;
            }

            if (request.HttpContext.RequestAborted.IsCancellationRequested)
                return "[Request aborted by client]";

            try
            {
                request.EnableBuffering();

                if (request.Body.CanSeek)
                    request.Body.Position = 0;

                using var reader = new StreamReader(
                    request.Body,
                    encoding,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true);

                var result = await reader.ReadToEndAsync(request.HttpContext.RequestAborted);

                return result;
            }
            catch (OperationCanceledException) when (request.HttpContext.RequestAborted.IsCancellationRequested)
            {
                return "[Request aborted by client]";
            }
            catch (Microsoft.AspNetCore.Connections.ConnectionResetException)
            {
                return "[Connection reset by remote host]";
            }
            catch (IOException)
            {
                return "[Failed to read request body]";
            }
            catch (ObjectDisposedException)
            {
                return "[Request body stream disposed]";
            }
            finally
            {
                try
                {
                    if (request.Body != null && request.Body.CanSeek)
                        request.Body.Position = 0;
                }
                catch
                {
                    // Ignore reset-position errors.
                }
            }
        }

        private async Task<string> DecryptRequestBody(string key, string iv, string body)
        {
            var aesHelper = new AES_Helper();
            return await Task.FromResult(aesHelper.Decrypt(key, iv, body));
        }

        private async Task<Stream> ConvertStringToStreem(string text)
        {
            var textAsBytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            return await Task.FromResult<Stream>(new MemoryStream(textAsBytes));
        }

        private async Task<bool> WriteErrorAndStopAsync(HttpContext context, int statusCode, string message)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(message);
            }

            return false;
        }

        public async Task<bool> PrepareRequest(HttpContext context)
        {
            try
            {
                var clientIntegrationPermission = context.Request.Headers["integrationPermission"].ToString();

                if (!(IsCypherActive &&
                      clientIntegrationPermission != IntegrationPermission &&
                      !context.Request.Path.ToString().Contains("HandShake", StringComparison.OrdinalIgnoreCase) &&
                      !IsExecludedApi(context.Request) &&
                      !IsExecludedHost(context.Request)))
                {
                    return true;
                }

                var isSecuredApiFlag = IsSecuredApi(context.Request);

                var pointer = context.Request.Headers["pointerId"].ToString();
                var signature = context.Request.Headers["signature"].ToString();
                var majorSalt = context.Request.Headers["majorSalt"].ToString();
                var oldContentType = context.Request.Headers["oldContentType"].ToString();

                if (isSecuredApiFlag && string.IsNullOrEmpty(majorSalt))
                {
                    return await WriteErrorAndStopAsync(context, 412, "Invalid Request!!!");
                }

                if (string.IsNullOrEmpty(pointer) || string.IsNullOrEmpty(signature))
                {
                    return await WriteErrorAndStopAsync(context, 412, "Invalid Request!!!");
                }

                var isWriteMethod = HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method);
                var requestContent = string.Empty;

                if (isSecuredApiFlag && isWriteMethod)
                {
                    if (!context.Request.HasFormContentType)
                    {
                        return await WriteErrorAndStopAsync(context, 412, "Invalid Request!!!");
                    }

                    var cipherText = context.Request.Form["cipherText"];
                    if (cipherText.Count == 0)
                    {
                        return await WriteErrorAndStopAsync(context, 412, "Invalid Request!!!");
                    }

                    requestContent = cipherText[cipherText.Count - 1];
                }
                else
                {
                    if (context.Request.HasFormContentType || oldContentType.Contains("formdata", StringComparison.OrdinalIgnoreCase))
                    {
                        if (context.Request.Headers["Content-type"] == "text/plain")
                        {
                            requestContent = await FormatRequest(context.Request);
                        }
                        else
                        {
                            var formData = new List<EskaCMS.Middlewares.ViewModels.KeyValuePair>();

                            foreach (var item in context.Request.Form)
                            {
                                formData.Add(new EskaCMS.Middlewares.ViewModels.KeyValuePair
                                {
                                    Key = item.Key,
                                    Value = item.Value
                                });
                            }

                            requestContent = JsonConvert.SerializeObject(formData);
                        }
                    }
                    else
                    {
                        requestContent = await FormatRequest(context.Request);
                    }
                }

                var cypherParams = await GetCypherParam(pointer);

                if (cypherParams == null ||
                    cypherParams.CreationDate.AddDays(EXPIRATION_PRIOD_IN_DAYS).CompareTo(DateTimeOffset.Now) < 0)
                {
                    return await WriteErrorAndStopAsync(context, 412, "Invalid Request!!!");
                }

                var body = requestContent;

                if (isSecuredApiFlag && isWriteMethod)
                {
                    body = await DecryptRequestBody(cypherParams.MajorKey, majorSalt, body);

                    if (oldContentType.Contains("formdata", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Request.Headers["Content-type"] =
                            "multipart/form-data; boundary=----WebKitFromBoundaryvSbfxqhU3HCG4H9A";

                        var formData = JsonConvert.DeserializeObject<List<EskaCMS.Middlewares.ViewModels.KeyValuePair>>(body)
                                       ?? new List<EskaCMS.Middlewares.ViewModels.KeyValuePair>();

                        var formDataDictionary = new Dictionary<string, StringValues>();

                        foreach (var item in formData)
                        {
                            if (!formDataDictionary.ContainsKey(item.Key))
                            {
                                formDataDictionary.Add(item.Key, item.Value);
                            }
                        }

                        foreach (var item in context.Request.Form)
                        {
                            if (item.Key != "cipherText" && !formDataDictionary.ContainsKey(item.Key))
                            {
                                formDataDictionary.Add(item.Key, item.Value);
                            }
                        }

                        var files = context.Request.Form.Files;

                        context.Request.Form = new FormCollection(formDataDictionary, files);

                        context.Request.Headers.Remove("Content-Length");
                    }
                    else
                    {
                        context.Request.Body = await ConvertStringToStreem(body);
                        context.Request.Body.Position = 0;

                        context.Request.Headers["Content-type"] = "application/vnd.api+json";
                        context.Request.Headers.Remove("Content-Length");
                    }
                }

                var bodyWithAuth = body + cypherParams.AuthId;
                var hashedCode = await GenerateHash(bodyWithAuth);

                if (hashedCode != signature)
                {
                    return await WriteErrorAndStopAsync(context, 400, "Bad Request!!!");
                }

                return true;
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                return false;
            }
            catch (Microsoft.AspNetCore.Connections.ConnectionResetException)
            {
                return false;
            }
            catch (IOException) when (context.RequestAborted.IsCancellationRequested)
            {
                return false;
            }
            catch (ObjectDisposedException) when (context.RequestAborted.IsCancellationRequested)
            {
                return false;
            }
            catch
            {
                throw;
            }
        }
    }
}
