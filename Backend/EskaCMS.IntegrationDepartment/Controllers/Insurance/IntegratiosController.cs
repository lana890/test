using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Integrations.Models.Insurance;
using EskaCMS.Integrations.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
namespace EskaCMS.Integrations.Controllers.Insurance
{//Categories

	[Route("api/Integrations")]
	[ApiController]
	[Authorize]
	public class IntegratiosController : ControllerBase
	{

		private readonly IIntegrationServices _integrationServices;
		private readonly IWorkContext _workContext;
		public IntegratiosController(IWorkContext workContext, IIntegrationServices integrationServices)
		{
			_integrationServices = integrationServices;
			_workContext = workContext;

		}
		[HttpGet]
		[Route("CreatePolicy")]
		public async Task<IActionResult> CreatePolicy(long orderId)
		{

			try
			{

				await _integrationServices.CreatePolicyAsync(orderId);

				return Ok();
			}

			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPost]
		[Route("RepushPolicies")]
		public async Task<IActionResult> RepushPolicies([FromBody] RepushPoliciesVM model)
		{
			if (model?.OrderIds == null || model.OrderIds.Count == 0)
				return BadRequest("OrderIds is required.");

			var results = new List<RepushResultVM>();

			// Sequential on purpose: the service shares a scoped DbContext, so parallel calls would corrupt it.
			foreach (var orderId in model.OrderIds.Distinct())
			{
				// RepushPolicyAsync never throws — it returns a per-order result (paid check,
				// map-existing-vs-create, status update all handled inside the service).
				results.Add(await _integrationServices.RepushPolicyAsync(orderId));
			}

			return Ok(results);
		}

		[HttpPost]
		[Route("CalculateQoutation")]
		public async Task<IActionResult> CalculateQoutation([FromBody] CalculateQoutationVM calculate)
		{
			try
			{
				var currentUserId = await _workContext.GetCurrentUserId();

				await _integrationServices.CalculateQoutation(currentUserId, calculate.cartId, calculate.Files);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}


		//[HttpGet]
		//[Route("CalculateQoutation")]
		//public async Task<IActionResult> CalculateQoutation(long CartId, [FromForm] List<IFormFile> files)
		//{

		//		try
		//		{
		//			var currentUserId = await _workContext.GetCurrentUserId();

		//			var base64Files = new List<string>();

		//			if (files != null && files.Count > 0)
		//			{
		//				foreach (var file in files)
		//				{
		//					using (var ms = new MemoryStream())
		//					{
		//						await file.CopyToAsync(ms);
		//						var fileBytes = ms.ToArray();

		//						var base64 = Convert.ToBase64String(fileBytes);
		//						base64Files.Add(base64);
		//					}
		//				}
		//			}

		//			await _integrationServices.CalculateQoutation(currentUserId, CartId, base64Files);

		//			return Ok();
		//		}
		//		catch (Exception ex)
		//		{
		//			return BadRequest(ex.Message);
		//		}
		//	}
		//}
	}
}
