using EskaCMS.Entities.Entity.EcomLog;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using EskaCommerce.ShoppingCart.EcommerceLog.ModelView;
using EskaCommerce.ShoppingCart.EcommerceLog.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EskaCommerce.ShoppingCart.EcommerceLog.Controllers
{
    [Area("EcommerceLog")]

    [ApiController]
    [Route("api/EcommerceLog")]
	[Authorize]
	public class EcommerceLogController : ControllerBase
    {
        private readonly IEcommerceLogService _ecomLogService;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EcommerceLogController> _logger;

        public EcommerceLogController(
            IEcommerceLogService ecomLogService,
        IHttpClientFactory httpClientFactory,
            ILogger<EcommerceLogController> logger)
        {
            _ecomLogService = ecomLogService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost("AddLogVersion")]
       
        public async Task<IActionResult> AddLogVersion([FromBody] AddVersionRequest ecomLogVersion)
        {
            try
            {
                if (ecomLogVersion == null)
                    return BadRequest(new { error = "Version data is required" });

                var result = await _ecomLogService.AddInitialVersionAsync(ecomLogVersion);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding log version");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("SetMainVersion")]
        
        public async Task<IActionResult> SetMainVersion(long logId, long versionId)
        {
            try
            {
                if (logId <= 0 || versionId <= 0)
                    return BadRequest(new { error = "Valid log ID and version ID are required" });

                var result = await _ecomLogService.SetMainVersion(logId, versionId);
                return Ok(new { message = "Log replaced successfully", log = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing log {LogId}", logId);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("GetAllLogs")]
         
        public async Task<IActionResult> GetAllLogs(SearchLogParam logParam)
        {
            try
            {
                var logs = await _ecomLogService.GetAllLogsAsync(logParam);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all logs");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("GetLogWithVersions/{logId}")]
        
        public async Task<IActionResult> GetLogWithVersions(long logId)
        {
            try
            {
                if (logId <= 0)
                    return BadRequest(new { error = "Valid log ID is required" });

                var log = await _ecomLogService.GetLogWithAllVersionsAsync(logId);
                if (log == null)
                    return NotFound(new { error = $"Log with ID {logId} not found" });

                return Ok(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting log {LogId}", logId);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("ReSubmitLog")]
      
        public async Task<IActionResult> ReSubmitLog(long logId, [FromBody] RequestPayload? request = null)
        {
            try
            {

                var success = await _ecomLogService.ReSubmitLogAsync(logId, request);

                return Ok(new
                {
                    success = success,
                    message = success ? "Log resubmitted successfully" : "Resubmission failed",
                    logId = logId
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resubmitting log {LogId}", logId);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("TriggerCartApi")]
        
        public async Task<IActionResult> TriggerCartApi([FromBody] TestCartRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "Request is required" });

            var client = _httpClientFactory.CreateClient();
            var externalApiUrl = "http://localhost:5000/api/cart/add-item";

            var payload = new
            {
                productId = request.ProductId,
                quantity = request.Quantity,
                cartType = request.CartType,
                additionalDetails = (string)null,
                planVersionId = (long?)null
            };

            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, externalApiUrl)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json"
                    )
                };

                httpRequest.Headers.TryAddWithoutValidation("siteId", "1");
                var response = await client.SendAsync(httpRequest);
                var responseBody = await response.Content.ReadAsStringAsync();

                return Ok(new
                {
                    success = response.IsSuccessStatusCode,
                    statusCode = (int)response.StatusCode,
                    response = responseBody,
                    message = "Request sent. Check EcommerceLogs table!"
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed");
                return StatusCode(500, new { error = $"HTTP Error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in test endpoint");
                return StatusCode(500, new
                {
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }



}