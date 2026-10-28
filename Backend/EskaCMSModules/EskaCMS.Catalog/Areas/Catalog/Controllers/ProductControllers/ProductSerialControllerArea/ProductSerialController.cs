using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Pricing;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductSerialArea.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
namespace EskaCommerce.Catalog.Areas.Catalog.Controllers
{
    [Route("api/ProductSerial")]
    [Authorize]
    public class ProductSerialController : Controller
    {
        private readonly IProductSerialService _productSerialService;
        // private readonly IWorkContext _workContext;
        // private readonly IRepository<ProductSerial> _productSerialRepository;

        public ProductSerialController(IProductSerialService productSerialService/* , IWorkContext workContext *//* , IRepository<ProductSerial> productSerialRepository */)
        {
            _productSerialService = productSerialService;
            // _workContext = workContext;
            // _productSerialRepository = productSerialRepository;
        }
        [HttpPost("GetSerials")]
        public async Task<IActionResult> GetProductSerials([FromBody] SmartTableParam Param)
        {
            try
            {
                SmartTableResult<ProductSerialViewModel> data = await _productSerialService.GetAllProductSerials(Param);
                return Ok(data);

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetProductSerialHistory")]
        public async Task<IActionResult> GetProductSerialHistory([FromQuery] long ProductSerialId)
        {
            try
            {


                List<ProductSerialHistoryViewModel> data = await _productSerialService.GetProductSerialHistory(ProductSerialId);
                return Ok(data);

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }


        [HttpDelete("DeleteSerial")]
        public async Task<IActionResult> DeleteSerial([FromQuery] long serialId)
        {
            try
            {
                await _productSerialService.ChangeProductSerialStatus(serialId, EProductSerialStatus.Deleted);
                return Ok();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


        [HttpPost("AddSerials")]
        public async Task<IActionResult> AddProductSerials([FromForm]IFormFile  formFile, [FromQuery] long ProductId)
        {
            try
            {

                List<ProductSerialViewModel> data = await _productSerialService.ImportFile(formFile);
                if (await _productSerialService.Create(data, ProductId))
                {
                    return Ok();
                }

                else return Forbid();

            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }

        }


        [HttpPost("AddSerialsFromRanges/{ProductId}")]
        public async Task<IActionResult> AddSerialsFromRanges([FromRoute] long ProductId,[FromBody]List<Range<long>> Ranges)
        {
            try
            {

                
               return Ok(await _productSerialService.AddSerialsFromRanges(ProductId,Ranges));

            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }

        }


        [HttpGet("GetSerialRanges/{ProductId}")]
        public async Task<IActionResult> GetSerialRanges([FromRoute] long ProductId)
        {
            try
            {


                return Ok(await _productSerialService.GetAllSerialsAsRanges(ProductId));

            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }

        }

        [HttpPost("DeleteSerialRange/{ProductId}")]
        public async Task<IActionResult> DeleteSerial([FromRoute] long ProductId,[FromBody] List<Range<long>> Ranges)
        {
            try
            {
                await _productSerialService.DeleteRange(ProductId, Ranges);
                return Ok();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


        [HttpPost("export")]
        public async Task<ActionResult<SerialsExportVm>> Export([FromBody] SmartTableParam Param)
        {
            try
            {
                var Result = await _productSerialService.GetSerialsToExport(Param);
                var csvString = CsvConverter.ExportCsv(Result);
                var csvBytes = Encoding.UTF8.GetBytes(csvString);

                var csvBytesWithUTF8BOM = Encoding.UTF8.GetPreamble().Concat(csvBytes).ToArray();

                return File(csvBytesWithUTF8BOM, "text/csv", "productSerial-export.csv");

            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }

        }


        [HttpGet]
        [Route("GetProductSerialTemplate")]
        public async Task<IActionResult> GetProductSerialTemplate()
        {
            try
            {

                ExcelFile Template = await ExcelHelper.ExportExcelTemplate<ProductSerialTemplateViewModel>("Product Serial Template");
                return File(Template.FileBytes, Template.ContentType, Template.Name);

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
        [HttpGet("ExportSerialsWithOwners/{ProdcutId}")]
        [AllowAnonymous]

        public async Task<ActionResult<SerialsExportVm>> ExportSerialsWithOwners([FromRoute] long ProdcutId)
        {
            try
            {
                var Result = await _productSerialService.GetSerialsWithOwners(ProdcutId);

                var file = await ExcelHelper.ExportExcelFile<SerialsExportWithOwnersVM>(Result, "Participants");
             

                return File(file.FileBytes, file.ContentType, file.Name);

            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }

        }

      
        
        
        [HttpPost("ReserveSerials/{ProductSerialId}/{OrderId}")]

        public async Task<IActionResult> ReserveSerials([FromRoute] long ProductSerialId, [FromRoute] long OrderId)
        {
            try
            {

                return Ok(await _productSerialService.ReserveSerials(ProductSerialId, OrderId));

            }
            catch (Exception ex)
            {

                throw ex;
            }
        
        
        }

        [HttpPost("RestoreSerials/{ProductSerialId}")]



        public async Task<IActionResult> RestoreSerials([FromRoute] long ProductSerialId)
        {
            try
            {

                return Ok(await _productSerialService.RestoreSerials(ProductSerialId));

            }
            catch (Exception ex)
            {

                throw ex;
            }


        }





    }

}

