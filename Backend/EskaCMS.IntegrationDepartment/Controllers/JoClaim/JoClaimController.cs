using Elastic.Clients.Elasticsearch.Analysis;
using EskaCMS.Core.Cache;
using EskaCMS.Core.Extensions;
using EskaCMS.Integrations.Helpers;
using EskaCMS.Integrations.Models.Carseer;
using EskaCMS.Integrations.Models.CBVehicleAssessment;
using EskaCMS.Integrations.Models.JIF;
using EskaCMS.Integrations.Services.JIF;
using EskaCMS.Integrations.Services.JoClaim.Interface;
using iText.Forms.Fields.Merging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EskaCMS.Integrations.Controllers.JoClaim
{
    [Route("api/JoClaim")]
    [ApiController]

    public class JoClaimController : ControllerBase
    {

        private readonly IJoClaimService _IJoClaimService;
        private readonly IWorkContext _workContext;
        private readonly EskaCMS.Integrations.Models.JoClaim.JoClaim _JoClaimSettings;
        private readonly CarseerSettings _CarseerSettings;

        public JoClaimController(IWorkContext workContext, IJoClaimService IJoClaimService)
        {
            _IJoClaimService = IJoClaimService;
            _workContext = workContext;
            _CarseerSettings = IntegrationSettingsLoader.Load<CarseerSettings>(AppContext.BaseDirectory, "CarseerSettings");

            _JoClaimSettings = IntegrationSettingsLoader.Load<EskaCMS.Integrations.Models.JoClaim.JoClaim>(AppContext.BaseDirectory, "JOClaimInteg");
        }
        [HttpPost]
        [Route("GetVehicleHistory")]
        public async Task<IActionResult> GetVehicleHistory([FromQuery] GetVehicleHistoryVM historyVM)
        {
            // Validate VIN
            if (string.IsNullOrWhiteSpace(historyVM.vin))
                return BadRequest(new { error = "VIN is required." });

            // Validate language
            if (historyVM.language != "en" && historyVM.language != "ar")
                return BadRequest(new { error = "Language must be 'en' or 'ar'." });

            try
            {
                var report = await _IJoClaimService.GetVehicleHistoryAsync(historyVM.vin, historyVM.language);
                return Ok(report);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, new { error = "Carseer API timed out." });
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, new { error = "Carseer API is unavailable." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        [HttpPost("CBVehicleAssessment/{siteId}/{cartId}")]
        public async Task<IActionResult> CBVehicleAssessment([FromBody] VehicleAssessmentRequest reportJson, long siteId , long cartId)
        {
            long userId = await _workContext.GetCurrentUserId();

            //if (reportJson.ValueKind == JsonValueKind.Undefined || reportJson.ValueKind == JsonValueKind.Null)
            //    return BadRequest(new { error = "Request body is required." });

            try
            {
                await _IJoClaimService.CBVehicleAssessment(userId, reportJson.report.CaseDamageReport.OverallStatus,cartId,siteId);
                return Ok(new { message = "Assessment processed successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { error = "An external service call failed." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
		[HttpPost("{cartId}")]
		public async Task<IActionResult> JoclaimInteg([FromBody] JoclaimIntegRequest request,long cartId)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                return BadRequest(new { error = "Vin is required." });

            if (string.IsNullOrWhiteSpace(request.PlateNo))
                return BadRequest(new { error = "PlateNo is required." });

            if (string.IsNullOrWhiteSpace(request.OwnerName))
                return BadRequest(new { error = "OwnerName is required." });

            if (string.IsNullOrWhiteSpace(request.OwnerPhone))
                return BadRequest(new { error = "OwnerPhone is required." });

            try
            {
               var aiLink= await _IJoClaimService.JoclaimInteg(request, cartId);
                return Ok(new {AiUrl=aiLink, message = "Joclaim integration completed successfully." });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { error = "An external service call failed." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }
}
