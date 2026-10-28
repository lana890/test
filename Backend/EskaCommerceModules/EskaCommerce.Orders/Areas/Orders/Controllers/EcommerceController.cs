
using EskaCMS.Entities.Entity.Orders.Models;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.UserDashboard.Services.Interfaces;
using EskaCMS.UserDashboard.ViewModels;
using EskaCommerce.Orders.ModelVM;
using EskaCommerce.Orders.ModelVM.Request;
using EskaCommerce.Orders.Services.EcomAnalticsServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;


namespace EskaCommerce.Orders.Areas.Orders.Controllers
{
    [Area("Orders")]

    [Route("api/EcommerceDashboard")]
    [ApiController]
    [Authorize]
    public class EcommerceController : ControllerBase
    {
        public IEcommerceService _ecomService { get; set; }
        public EcommerceController(
                IEcommerceService ecomService
            )
        {
            _ecomService = ecomService;
        }

        [HttpPost]
        [Route("Income")]
        public async Task<IActionResult> Income([FromBody] TimeRequest timeRequest)
        {
            try
            {
                return Ok(await _ecomService.Income(timeRequest.time, timeRequest.search));
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [HttpPost]
        [Route("GetCartOrderCharts")]
        public async Task<IActionResult> GetCartOrderCharts([FromBody] TimeRequest timeRequest)
        {
            try
            {
                List<CartOrderChart> results = await _ecomService.GetCartOrderCharts(timeRequest.time, timeRequest.search);
                return Ok(results);
            }
            catch (Exception)
            {
                throw;
            }
        }
        [HttpPost]
        [Route("ConversionRate")]
        public async Task<IActionResult> ConversionRate([FromBody] TimeRequest timeRequest)
        {
            try
            {
                return Ok(await _ecomService.ConversionRate(timeRequest.time, timeRequest.search));
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [HttpPost]
        [Route("TotalOrders")]
        public async Task<IActionResult> TotalOrders([FromBody] TimeRequest timeRequest)
        {
            try
            {
                return Ok(await _ecomService.TotalOrders(timeRequest.time, timeRequest.search));
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [HttpPost]
        [Route("AbondomentRate")]
        public async Task<IActionResult> AbondomentRate([FromBody] TimeRequest timeRequest)
        {
            try
            {
                return Ok(await _ecomService.AbondomentRate(timeRequest.time, timeRequest.search));
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [HttpPost]
        [Route("MostSearchedProducts")]
        public async Task<IActionResult> MostSearchedProducts([FromBody] TimeRequest timeRequest)

        {
            try
            {
                return Ok(await _ecomService.MostSearchedProducts(timeRequest.time, timeRequest.search));
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("CustomerAnalyticsDashboard")]
        public async Task<IActionResult> CustomerAnalyticsDashboard([FromBody] TimeRequest timeRequest)
        {
            try
            {
                return Ok(await _ecomService.CustomerAnalyticsDashboard(timeRequest.time, timeRequest.search));
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [HttpPost]
        [Route("TrafficAnalytic")]
        public async Task<IActionResult> AbandonmentByTrafficSource([FromBody] TimeRequest timeRequest)
        {
            try
            {
                return Ok(await _ecomService.AbandonmentByTrafficSource(timeRequest.time, timeRequest.search));
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [HttpPost]
        [Route("AbandonmentByDevice")]
        public async Task<IActionResult> AbandonmentByDevice([FromBody] TimeRequest timeRequest)
        {
            try
            {
                return Ok(await _ecomService.AbandonmentByDevice(timeRequest.time, timeRequest.search));
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        [HttpPost]
        [Route("Sales-funnel")]
        public async Task<IActionResult> SalesFunnel([FromBody] SalesFunnelRequest request)
        {
            try
            {
                return Ok(await _ecomService.SalesFunnel(request.filter, request.timeRequest.time, request.timeRequest.search));
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [HttpPost]
        [Route("ClickThroughRate")]
        public async Task<IActionResult> ClickThroughRate([FromBody] TimeRequest timeRequest)
        {
            try
            {
                return Ok(await _ecomService.ClickThroughRate(timeRequest.time, timeRequest.search));
            }
            catch (Exception ex)
            {
                throw;
            }

        }
        [HttpPost]
        [Route("SourceTraffic")]
        public async Task<IActionResult> SourceTraffic([FromBody] TimeRequest timeRequest)
        {
            try
            {
                return Ok(await _ecomService.ProductTrafficSource(timeRequest.time, timeRequest.search));
            }
            catch (Exception ex)
            {
                throw;
            }

        }
        [HttpPost]
        [Route("Product-Views")]
        public async Task<IActionResult> Product_Views([FromBody] ProductViewsRequest ViewFilter)
        {
            try
            {
                return Ok(await _ecomService.Product_Views(ViewFilter.ProductId, ViewFilter.timeRequest.time, ViewFilter.timeRequest.search));
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("product-insights")]
        public async Task<IActionResult> product_insights([FromBody] TimeRequest timeRequest)
        {
            try
            {
                return Ok(await _ecomService.product_insights(timeRequest.time, timeRequest.search));
    }
            catch (Exception ex)
            {
                throw;
            }
        }

        //[HttpGet]
        //[Route("AbondmentRateItems")]
        //public async Task<IActionResult> AbondmentRateItems(TimeIntervals time)
        //{
        //    try
        //    {
        //        return Ok(await _ecomService.AbondmentRateItems(time));
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}
    }
}
