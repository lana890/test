using EskaCommerce.LoyaltyPoints.Models.Request;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.LoyaltyPoints.Controllers
{
    [Route("api/LoyaltyPointsConversion/[action]")]
    [ApiController]
    [Authorize]
    public class PointsConversionControllers : Controller
    {
        private readonly IPointsConversionService _pointsConversionService;
        public PointsConversionControllers(IPointsConversionService pointsConversionService)
        {
            _pointsConversionService = pointsConversionService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PointsConversionDto model)
        {
            try
            {
                var result =  await _pointsConversionService.CreatePointsConversion(model);
                if (result)
                    return Ok("Points conversion has been added");

                return BadRequest("Failed to create points conversion");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] PointsConversionDto model )
        {
            try
            {
                var result = await _pointsConversionService.UpdatePointsConversion(model);
                if (result)
                    return Ok("Points conversion has been updated");

                return BadRequest("Failed to update points conversion");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpGet]
        public async Task<IActionResult> PointsConversion()
        {
            try
            {
                var result = await _pointsConversionService.GetPointsConversion();
                return Ok(result);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpDelete]
        public async Task<IActionResult> DeletePointsConversion()
        {
            try
            {
                var result = await _pointsConversionService.DeletePointsConversion();
                return Ok(result);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }




    }
}
