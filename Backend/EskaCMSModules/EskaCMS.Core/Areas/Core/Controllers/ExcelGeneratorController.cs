using EskaCMS.Core.Areas.Core.Interfaces;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Infrastructure.Helpers.ExcelHelper;

namespace EskaCMS.Core.Areas.Core.Controllers
{
    [ApiController]
    [Route("api/ExcelGenerator/[action]")]
    public class ExcelGeneratorController : ControllerBase
    {
        private readonly IExcelGenerateServices excelGenerateServices;

        public ExcelGeneratorController(IExcelGenerateServices excelGenerateServices)
        {
            this.excelGenerateServices = excelGenerateServices;
        }

        [HttpPost]
        public async Task<IActionResult> Generate([FromBody] ViewModels.ExcelDataVm model)
        {
            try
            {

                var  Result=await excelGenerateServices.Generate(model);
                return File(Result.FileBytes, Result.ContentType, Result.Name);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
    }
}
