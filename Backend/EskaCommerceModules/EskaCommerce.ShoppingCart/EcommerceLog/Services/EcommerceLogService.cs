using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Snapshot;
using EskaCMS.Core.Extensions;
using EskaCMS.DataAccess.Migrations;
using EskaCMS.Entities.Entity.EcomLog;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.ShoppingCart.EcommerceLog.ModelView;
using EskaCommerce.ShoppingCart.EcommerceLog.Services.Interface;
using EskaCommerce.ShoppingCart.Services;
using Ical.Net.DataTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries.Utilities;
using Org.BouncyCastle.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static Google.Apis.Storage.v1.ChannelsResource;
namespace EskaCommerce.ShoppingCart.EcommerceLog.Services
{
    public class EcommerceLogService : IEcommerceLogService
    {
        private readonly IRepository<EcommerceLogs> _ecoLogRepository;
        private readonly IRepository<EcommerceLogsVersions> _ecoVersionRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EcommerceLogService> _logger;
        private readonly ICartService _cartService;
        private readonly IWorkContext _workContext;
        private readonly IConfiguration _config;
        public EcommerceLogService(
            IRepository<EcommerceLogs> ecoLogRepository,
            IHttpClientFactory httpClientFactory,
            ILogger<EcommerceLogService> logger,
            ICartService cartService,
           IRepository<EcommerceLogsVersions> ecoVersionRepository,
           IConfiguration config,
           IWorkContext workContext)
        {
            _ecoLogRepository = ecoLogRepository;
            _ecoVersionRepository = ecoVersionRepository;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _cartService = cartService;
            _config = config;
            _workContext = workContext;
        }

        public async Task<EcommerceLogs> CreateLogAsync(EcommerceLogs log)
        {
            try
            {
                if (log == null)
                    throw new ArgumentNullException(nameof(log), "Log cannot be null");

                _ecoLogRepository.Add(log);
                await _ecoLogRepository.SaveChangesAsync();
                return log;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<PaginationResult<EcommerceLogWithVersionsVM>> GetAllLogsAsync(SearchLogParam logParam)
        {
            try
            {
                TransactionStatus? status = logParam.status;
                string searchValue = logParam.SearchValue ?? null;

                var baseQuery = _ecoLogRepository.Query()
                    .Where(x =>
                        (status == 0 || x.Status == status) &&
                        (string.IsNullOrEmpty(searchValue) ||
                            x.UserId.ToString().Contains(searchValue) ||
                            x.OrderId.ToString().Contains(searchValue) ||
                            (!string.IsNullOrEmpty(x.User.UserName) && x.User.UserName.Contains(searchValue)) ||
                            (!string.IsNullOrEmpty(x.User.FullName) && x.User.FullName.Contains(searchValue)) ||
                            (!string.IsNullOrEmpty(x.User.Email) && x.User.Email.Contains(searchValue)) ||
                            x.Cart.Items.Any(i =>
                               !string.IsNullOrEmpty(i.AdditionalDetails) &&
                                i.AdditionalDetails.Contains(searchValue)
                            )
                        )
                    );

                int pageSize = logParam.PageSize == 0 ? 20 : logParam.PageSize;
                int pageNumber = logParam.PageNumber == 0 ? 1 : logParam.PageNumber;
                int totalCount = await baseQuery.CountAsync();

                var data = await baseQuery
                    .OrderByDescending(e => e.CreationDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new EcommerceLogWithVersionsVM
                    {
                        Id = x.Id,
                        CartId = x.CartId,
                        OrderId = x.OrderId,
                        SiteId = x.SiteId,
                        CreatedDate = x.CreationDate,
                        CurrentVersionId = x.CurrentVersionId,
                        Status = x.Status,
                        UserName = x.User != null ? x.User.FullName : null,
                        Versions = x.Versions
                            .Where(v => v.Id == x.CurrentVersionId)
                            .Select(v => new EcommerceLogVersionsVM
                            {
                                Id = v.Id,
                                Header = v.Header,
                                Method = v.Method,
                                Request = v.Request,
                                Response = v.Response,
                                URL = v.URL,
                                StatusCode = v.StatusCode,
                                UpdatedDate = v.ModificationDate
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return new PaginationResult<EcommerceLogWithVersionsVM>(
                    data,
                    (int)Math.Ceiling(totalCount / (double)pageSize),
                    totalCount
                );
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<EcommerceLogWithVersionsVM> GetLogWithAllVersionsAsync(long id)
        {
            try
            {
                return await _ecoVersionRepository.Query()
                        .Include(e => e.CreatedBy)
                        .Include(e => e.EcomLog)
                        .Where(e => e.EcomLogId == id)
                        .Select(x => new EcommerceLogWithVersionsVM
                        {
                            Id = x.EcomLogId,
                            CartId = x.EcomLog.CartId,
                            OrderId = x.EcomLog.OrderId,
                            SiteId = x.EcomLog.SiteId,
                            CreatedDate = x.EcomLog.CreationDate,
                            Status = x.EcomLog.Status,
                            UserName = x.EcomLog.User != null ? x.EcomLog.User.FullName : null,
                            CurrentVersionId = x.EcomLog.CurrentVersionId,
                            Versions = x.EcomLog.Versions
                                .OrderByDescending(v => v.Id)
                                .Select(v => new EcommerceLogVersionsVM
                                {
                                    Id = v.Id,
                                    Request = v.Request,
                                    Response = v.Response,
                                    Header = v.Header,
                                    Method = v.Method,
                                    URL = v.URL,
                                    StatusCode = v.StatusCode,
                                    UpdatedDate = v.ModificationDate,
                                    IsCurrent = v.Id == x.EcomLog.CurrentVersionId
                                }).ToList()
                        })
                        .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<EcommerceLogs> SetMainVersion(long logId, long versionId)
        {
            try
            {
                var currentLog = await GetLogCurrentVersionAsync(logId);
                if (currentLog == null)
                    throw new KeyNotFoundException($"EcommerceLog with ID {logId} not found");

                var targetVersion = await GetById(versionId);
                if (targetVersion == null)
                    throw new KeyNotFoundException($"Version with ID {versionId} not found");

                if (targetVersion.EcomLogId != logId)
                    throw new InvalidOperationException("Version does not belong to this log");

                currentLog.CurrentVersionId = versionId;
                currentLog.ModificationDate = DateTime.UtcNow;

                _ecoLogRepository.Update(currentLog);
                await _ecoLogRepository.SaveChangesAsync();

                return currentLog;
            }
            catch (Exception ex) { throw; }
        }

        public async Task<bool> ReSubmitLogAsync(long ecomLogId, RequestPayload? request = null)
        {

            var userId = await _workContext.GetCurrentUserId();
            var siteId = await _workContext.GetCurrentSiteIdAsync();
            var log = await GetLogCurrentVersionAsync(ecomLogId);
            var cartId = _workContext.GetCartId();

            if (log == null)
                throw new KeyNotFoundException($"EcommerceLog with ID {ecomLogId} not found");

            var currentVersion = log.Versions.FirstOrDefault(x => x.Id == log.CurrentVersionId);

            if (currentVersion == null)
                throw new InvalidOperationException("No current version found");

            try
            {
                var currentHeaders = string.IsNullOrWhiteSpace(currentVersion.Header)
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(currentVersion.Header)
                      ?? new Dictionary<string, string>();

                RequestPayload requestPayload;

                if (request == null)
                {
                    //If No request provided
                    requestPayload = new RequestPayload
                    {
                        Body = currentVersion.Request,
                        Headers = currentHeaders,
                        Method = currentVersion.Method,
                        Url = currentVersion.URL
                    };
                }
                else
                {
                    requestPayload = new RequestPayload
                    {
                        Url = !string.IsNullOrWhiteSpace(request.Url)
                            ? request.Url
                            : currentVersion.URL,

                        Method = !string.IsNullOrWhiteSpace(request.Method)
                            ? request.Method
                            : currentVersion.Method,

                        Body = !string.IsNullOrWhiteSpace(request.Body)
                            ? request.Body
                            : currentVersion.Request,

                        Headers = request.Headers != null && request.Headers.Any()
                            ? request.Headers
                            : currentHeaders
                    };

                    _logger.LogInformation(
                        "Merged request for log {LogId}: ProvidedUrl={ProvidedUrl}, ProvidedMethod={ProvidedMethod}, " +
                        "FinalUrl={FinalUrl}, FinalMethod={FinalMethod}",
                        ecomLogId,
                        request.Url ?? "null",
                        request.Method ?? "null",
                        requestPayload.Url,
                        requestPayload.Method
                    );
                }

                if (!requestPayload.IsValid)
                    throw new InvalidOperationException("Invalid request data - URL and Method are required");



                // Send request
                var headersJson = JsonSerializer.Serialize(requestPayload.Headers);
                var httpResponse = await SendRequestAsync(requestPayload.Body, headersJson, requestPayload.Url, requestPayload.Method);
                string responseBody = await httpResponse.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    responseBody = httpResponse.ReasonPhrase;
                }
                var (status, statusCode, cleanRsponse) = ParseResponseJson(responseBody, (int)httpResponse.StatusCode);

                var cart = await ExtractActiveCart(userId, siteId,cartId);


                await UpdateLogStatusAsync(
                    log.Id,
                   status,
                    cart,
                    cleanRsponse,
                    statusCode,
                    requestPayload,
                    request != null // Create new version if manual retry
                );
                return httpResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resubmitting log {LogId}", ecomLogId);
                updateRetrySchedule(log);
                await UpdateLogWithErrorAsync(log.Id, ex);
                throw;

            }

        }


        public async Task UpdateLogStatusAsync(long logId, TransactionStatus status, Cart? cart, string response, int statusCode, RequestPayload? payload = null, bool? IsVersionExecuted = false)

        {
            var orderId = cart?.Orders?.OrderByDescending(x => x.CreatedOn).Select(x => x.Id).FirstOrDefault();

            var log = await GetLogCurrentVersionAsync(logId);
            EcommerceLogsVersions? newVersion = null;
            var userId = await _workContext.GetCurrentUserId();
            try
            {
                if (log == null)
                    throw new KeyNotFoundException($"EcommerceLog with ID {logId} not found");

                var currentVersion = log.Versions.FirstOrDefault(x => x.Id == log.CurrentVersionId);
                string? cleanResponse = null;
                if (currentVersion == null)
                    throw new InvalidOperationException("No current version found");
                if (!string.IsNullOrWhiteSpace(response) && (response.TrimStart().StartsWith("{") || response.TrimStart().StartsWith("[")))
                {
                    var jsonDoc = JsonDocument.Parse(response);
                    var root = jsonDoc.RootElement;

                    //Check if the actuall status code inside the body
                    if (status == TransactionStatus.Success)
                    {
                        if (root.TryGetProperty("status", out var innerStatus))
                        {
                            int? statusBody = innerStatus.GetInt32();
                            if (statusBody.HasValue)
                            {
                                status = statusBody >= 200 && statusBody < 300 ? TransactionStatus.Success : TransactionStatus.Failed;
                                statusCode = (int)statusBody;
                            }

                        }
                    }
                    if (root.TryGetProperty("message", out var messageElement))
                    {
                        cleanResponse = messageElement.GetString();
                    }
                    else if (root.TryGetProperty("Message", out var messageElement2))
                    {
                        cleanResponse = messageElement2.GetString();
                    }
                }
                var newVersionRequest = new AddVersionRequest
                {
                    EcomLogId = log.Id,
                    Request = payload.Body,
                    Header = JsonSerializer.Serialize(payload.Headers),
                    Method = payload.Method,
                    Url = payload.Url,
                    Response = response,
                    StatusCode = statusCode,

                };
                if (IsVersionExecuted.Value)
                {
                    newVersion = await AddInitialVersionAsync(newVersionRequest);
                }
                else
                {
                    currentVersion.Response = response;
                    currentVersion.StatusCode = statusCode;

                    _ecoVersionRepository.Update(currentVersion);
                    await _ecoVersionRepository.SaveChangesAsync();
                }
                if (status == TransactionStatus.Success)
                {
                    log.RetryCount = 0;
                    log.NextRetryAt = null;
                }
                else
                {
                    updateRetrySchedule(log);
                }
                // Update log
                log.Status = status;
                log.CurrentVersionId = newVersion?.Id ?? log.CurrentVersionId;
                log.CartId = cart?.Id ?? null;
                log.OrderId = orderId;
                log.ModificationDate = DateTime.UtcNow;

                _ecoLogRepository.Update(log);
                await _ecoLogRepository.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                updateRetrySchedule(log);
                await UpdateLogWithErrorAsync(log.Id, ex);

                throw;
            }
        }



        public async Task<EcommerceLogsVersions> GetById(long versionId)
        {
            try
            {
                return await _ecoVersionRepository.GetById(versionId);
            }
            catch (Exception ex)
            {
                throw;
            }


        }
        public async Task<EcommerceLogsVersions> AddInitialVersionAsync(AddVersionRequest version)
        {
            try
            {
                if (version == null)
                    throw new ArgumentNullException(nameof(version));

                var userId = await _workContext.GetCurrentUserId();
                var logVersion = new EcommerceLogsVersions
                {
                    EcomLogId = version.EcomLogId,
                    Header = version.Header,
                    Method = version.Method,
                    Request = version.Request,
                    Response = version.Response,
                    StatusCode = version.StatusCode,
                    ModificationDate = DateTime.UtcNow,
                    URL = version.Url,
                    CreatedById = userId
                };

                _ecoVersionRepository.Add(logVersion);
                await _ecoVersionRepository.SaveChangesAsync();
                return logVersion;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private async Task<HttpResponseMessage> SendRequestAsync(
      string body,
      string logHeader,
      string url,
      string method)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                    throw new ArgumentNullException(nameof(url), "URL cannot be empty");

                var client = _httpClientFactory.CreateClient();

                // Parse HTTP method
                var httpMethod = method?.ToUpper() switch
                {
                    "GET" => HttpMethod.Get,
                    "POST" => HttpMethod.Post,
                    "PUT" => HttpMethod.Put,
                    "DELETE" => HttpMethod.Delete,
                    "PATCH" => HttpMethod.Patch,
                    _ => HttpMethod.Post
                };

                var httpRequest = new HttpRequestMessage(httpMethod, url);

                // Parse headers first — we need Content-Type to decide how to build body
                Dictionary<string, string> headers = new();
                if (!string.IsNullOrEmpty(logHeader))
                {
                    try
                    {
                        headers = JsonSerializer.Deserialize<Dictionary<string, string>>(logHeader)
                                  ?? new Dictionary<string, string>();
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("Failed to parse logHeader: {Error}", ex.Message);
                    }
                }

                // Detect content type from headers
                var contentType = headers.TryGetValue("Content-Type", out var ct)
                    ? ct : string.Empty;

                // Build body based on content type
                if (httpMethod != HttpMethod.Get && !string.IsNullOrEmpty(body))
                {
                    if (contentType.Contains("multipart/form-data"))
                    {
                        httpRequest.Content = BuildMultipartContent(body, contentType);
                    }
                    else if (contentType.Contains("application/x-www-form-urlencoded"))
                    {
                        httpRequest.Content = BuildFormUrlEncodedContent(body);
                    }
                    else
                    {
                        httpRequest.Content = new StringContent(
                            body,
                            Encoding.UTF8,
                            contentType.Contains("application/json")
                                ? "application/json"
                                : "application/json");
                    }
                }

                foreach (var header in headers)
                {
                    if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (IsContentHeader(header.Key))
                        httpRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    else
                        httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                httpRequest.Headers.TryAddWithoutValidation("X-Skip-Logging", "true");

                var response = await client.SendAsync(httpRequest);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend request to {Url}", url);
                throw;
            }
        }


        private MultipartFormDataContent BuildMultipartContent(string body, string contentType)
        {
            try
            {

                var boundary = string.Empty;
                var parts = contentType.Split(';');
                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (trimmed.StartsWith("boundary=", StringComparison.OrdinalIgnoreCase))
                    {
                        boundary = trimmed.Substring("boundary=".Length).Trim('"');
                        break;
                    }
                }

                var multipart = string.IsNullOrEmpty(boundary)
                    ? new MultipartFormDataContent()
                    : new MultipartFormDataContent(boundary);

                // body was captured as raw multipart string
                // Parse each part by looking for known field patterns in JSON
                // Since the original body was logged as raw string, try to parse as JSON fields
                try
                {
                    var jsonFields = JsonSerializer.Deserialize<Dictionary<string, string>>(body);
                    if (jsonFields != null)
                    {
                        foreach (var field in jsonFields)
                        {
                            multipart.Add(
                                new StringContent(field.Value, Encoding.UTF8, "application/json"),
                                field.Key);
                        }
                        return multipart;
                    }
                }
                catch
                {
                    // Not JSON — fall through to raw string
                }

                // Fallback: add entire body as single field
                multipart.Add(new StringContent(body, Encoding.UTF8), "data");
                return multipart;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to rebuild multipart content: {Error}", ex.Message);
                // Fallback to plain JSON
                return new MultipartFormDataContent
        {
            { new StringContent(body, Encoding.UTF8, "application/json"), "data" }
        };
            }
        }

        // ===================== FORM URL ENCODED BUILDER =====================

        private FormUrlEncodedContent BuildFormUrlEncodedContent(string body)
        {
            try
            {
                var fields = JsonSerializer.Deserialize<Dictionary<string, string>>(body)
                             ?? new Dictionary<string, string>();
                return new FormUrlEncodedContent(fields);
            }
            catch
            {
                // Fallback: parse as query string  
                var pairs = body.Split('&')
                    .Select(p => p.Split('='))
                    .Where(p => p.Length == 2)
                    .Select(p => new KeyValuePair<string, string>(
                        Uri.UnescapeDataString(p[0]),
                        Uri.UnescapeDataString(p[1])));

                return new FormUrlEncodedContent(pairs);
            }
        }

        // ===================== HELPERS =====================

        private static readonly HashSet<string> _contentHeaders =
            new(StringComparer.OrdinalIgnoreCase)
        {
    "Content-Type",
    "Content-Length",
    "Content-Encoding",
    "Content-Language",
    "Content-Location",
    "Content-MD5",
    "Content-Range",
    "Content-Disposition",
    "Expires",
    "Last-Modified"
        };

        private static bool IsContentHeader(string headerName) =>
            _contentHeaders.Contains(headerName);

        private void updateRetrySchedule(EcommerceLogs log)
        {

            var _retryConfig = _config.GetSection("RetryConfig").Get<RetryConfig>() ?? new RetryConfig();
            log.RetryCount++;
            //each time the wait time exceeded so the server would recover
            var delaySec = _retryConfig.RetryDelaySec * (int)Math.Pow(2, log.RetryCount - 1);
            log.NextRetryAt = DateTime.UtcNow.AddSeconds(delaySec);
        }
        private async Task<EcommerceLogs> GetLogCurrentVersionAsync(long id)
        {
            try
            {

                var log = await _ecoLogRepository.Query().
                   Include(e => e.Versions)
                    .Include(e => e.Order)
                    .Include(e => e.Cart)
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == id);


                return log;
            }
            catch (Exception)
            {
                throw;
            }
        }
        //return all logs that didnt exceeded the maxRetry count and failed with certaine statuscodes 
        public async Task<List<EcommerceLogsVersions>> GetLogsForRetryAsync(int maxRetry)
        {
            try
            {
                if (maxRetry <= 0) new List<EcommerceLogsVersions>();
                return await _ecoVersionRepository.Query().Include(x => x.EcomLog).Where(x =>
                 x.EcomLog.Status == TransactionStatus.Failed
                 && x.EcomLog.CurrentVersionId == x.Id
                 && x.EcomLog.RetryCount < maxRetry
                 && (x.EcomLog.NextRetryAt == null || x.EcomLog.NextRetryAt <= DateTime.UtcNow)
                 && (x.StatusCode == 408
                 || x.StatusCode == 429
                 || x.StatusCode == 502
                 || x.StatusCode == 503
                 || x.StatusCode == 504)
                 ).OrderBy(x => x.EcomLog.NextRetryAt).ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        private (TransactionStatus status, int statusCode, string cleanResponse) ParseResponseJson(string response, int httpStatusCode)
        {
            var status = httpStatusCode >= 200 && httpStatusCode < 300
                ? TransactionStatus.Success
                : TransactionStatus.Failed;

            int statusCode = httpStatusCode;
            string cleanResponse = response;

            if (!string.IsNullOrWhiteSpace(response) &&
                (response.TrimStart().StartsWith("{") || response.TrimStart().StartsWith("[")))
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(response);
                    var root = jsonDoc.RootElement;

                    // Extract inner status code
                    if (status == TransactionStatus.Success &&
                        root.TryGetProperty("status", out var innerStatus))
                    {
                        int? statusBody = innerStatus.GetInt32();
                        if (statusBody.HasValue)
                        {
                            status = statusBody >= 200 && statusBody < 300
                                ? TransactionStatus.Success
                                : TransactionStatus.Failed;
                            statusCode = statusBody.Value;
                        }
                    }

                    // Extract clean message
                    if (root.TryGetProperty("message", out var messageElement))
                    {
                        cleanResponse = messageElement.GetString();
                    }
                    else if (root.TryGetProperty("Message", out var messageElement2))
                    {
                        cleanResponse = messageElement2.GetString();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse response JSON");
                }
            }

            return (status, statusCode, cleanResponse ?? response);
        }


        private async Task<Cart?> ExtractActiveCart(long userId, long siteId,long? cartId)
        {
            try
            {
                var cart = await _cartService.GetActiveCart(userId, CartTypes.Shop, siteId,cartId);
                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to extract cart for userId={UserId}, siteId={SiteId}",
                    userId, siteId);
                return null;
            }
        }

        // Updates log with error information.

        public async Task UpdateLogWithErrorAsync(long logId, Exception ex)
        {
            try
            {
                await UpdateLogStatusAsync(
                    logId,
                    TransactionStatus.Failed,
                    null,
                    $"Error: {ex.Message}\nStackTrace: {ex.StackTrace}",
                    (int)HttpStatusCode.InternalServerError,
                    null,
                    false
                );
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx,
                    "Failed to update log {LogId} with error",
                    logId);
            }
        }

        public async Task<EcommerceLogs> CreateLogWithResponseAsync(RequestPayload request, string response, int httpStatusCode, long userId, long siteId,long? cartId)
        {
            try
            {
                // Parse response
                var (status, statusCode, cleanResponse) = ParseResponseJson(response, httpStatusCode);

                // Extract cart and order
                var cart = await ExtractActiveCart(userId, siteId,cartId);
            //    var cartId = cart?.Id;
                long? orderId = cart?.Orders?
                    .OrderByDescending(x => x.CreatedOn)
                    .Select(x => x.Id)
                    .FirstOrDefault();

                // Create parent log
                var log = new EcommerceLogs
                {
                    Status = status,
                    CreationDate = DateTime.UtcNow,
                    UserId = userId,
                    SiteId = siteId,
                    CartId = cart?.Id,
                    OrderId = orderId == 0 ? null :orderId,
                    CurrentVersionId = 0
                };

                var createdLog = await CreateLogAsync(log);

                // Create initial version
                var initialVersion = new AddVersionRequest
                {
                    EcomLogId = createdLog.Id,
                    Request = request.Body,
                    Header = JsonSerializer.Serialize(request.Headers),
                    Method = request.Method,
                    Url = request.Url,
                    StatusCode = statusCode,
                    Response = cleanResponse
                };

                var addedVersion = await AddInitialVersionAsync(initialVersion);

                // Link version to log
                await SetMainVersion(createdLog.Id, addedVersion.Id);

                _logger.LogInformation("Log created: LogId={LogId}, Status={Status}, CartId={CartId}, OrderId={OrderId}", createdLog.Id, status, cartId, orderId);

                return createdLog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating log with response");
                throw;
            }
        }


    }
}