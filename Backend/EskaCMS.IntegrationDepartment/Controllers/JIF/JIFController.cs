using EskaCMS.Core.Cache;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Integrations.Helpers;
using EskaCMS.Integrations.Models.Insurance;
using EskaCMS.Integrations.Models.JIF;
using EskaCMS.Integrations.Services.Interfaces;
using EskaCMS.Integrations.Services.JIF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;
using static EskaCMS.Integrations.Models.JIF.VehicleInquiryViewModel;
namespace EskaCMS.Integrations.Controllers.Insurance
{//Categories

	[Route("api/JIF")]
	[ApiController]
 
	public class JIFController : ControllerBase
	{ 

		private readonly IJIFService _jIFService;
		// private readonly IWorkContext _workContext;
		// private readonly ICacheService _cacheService;
		private readonly JIFSettings _settings;

		public JIFController(/* IWorkContext workContext, */ IJIFService jIFService/* , ICacheService cacheService */)
		{
			_jIFService = jIFService;
			// _workContext = workContext;
			// _cacheService = cacheService;
			_settings = IntegrationSettingsLoader.Load<JIFSettings>(AppContext.BaseDirectory, "JIF");

		}
		[HttpPost]
		[Route("GetToken")]
		public async Task<IActionResult> GetToken()
		{

			try
			{
				string res =await _jIFService.GetToken();
				 
 				return Ok(res);
			}

			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}


		[HttpPost]
		[Route("GetVehicle")]
		public async Task<IActionResult> GetVehicle([FromBody] VehicleInquiryViewModel model)
		{
			try
			{
				if (!ModelState.IsValid)
				{
					return BadRequest(ModelState);
				}


				VehicleResponseViewModel res =	await _jIFService.GetVehicle(model);
				return Ok(res);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}




		[HttpPost]
		[Route("GetPolicy")]
		public async Task<IActionResult> GetPolicy([FromBody] PolicyRequestViewModel model)
		{
			try
			{
				//if (!ModelState.IsValid)
				//{
				//	return BadRequest(ModelState);
				//}


				PolicyResponseViewModel res = await _jIFService.GetPolicy(model);
				return Ok(res);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}




	}
}
