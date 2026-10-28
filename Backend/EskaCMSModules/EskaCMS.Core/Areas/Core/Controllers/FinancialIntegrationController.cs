using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Helpers.DinToPdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml.Drawing.Slicer.Style;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FinancialIntegrationController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IpdfConverter converter;
        private readonly IRepositoryWithTypedId<SiteSettings, string> appsettingsRepository;

        public FinancialIntegrationController(HttpClient httpClient, IpdfConverter converter, IRepositoryWithTypedId<SiteSettings, string> appsettingsRepository)
        {
            _httpClient = httpClient;
            this.converter=converter;
            this.appsettingsRepository=appsettingsRepository;
        }

        [HttpPost("GetCustomerStatement")]
        public async Task<IActionResult> GetCustomerStatement([FromBody] CustomerStatementRequest request)
        {
            try
            {

                var response = await GetCustomerStatementSOA(request);

                if (request.ResponseType==EskaCMS.Entities.Enum.GeneralEnums.EResponseType.PDF)
                {
                    var result = converter.GenerateTableHtml(response.Data);

                    return File(result.FileBytes, result.ContentType, result.Name);

                }
                else if (request.ResponseType==EskaCMS.Entities.Enum.GeneralEnums.EResponseType.Excel)
                {
                    var file = await ExcelHelper.ExportExcelFile(response.Data, "CustomerStatement");
                    return File(file.FileBytes, file.ContentType, file.Name);
                }
                else
                {
                    return Ok(response);

                }

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }



        private async Task<CustomerStatementResponse> GetCustomerStatementSOA(CustomerStatementRequest request)
        {
            var settings = await GetFinancialIntegrationSettings();

            _httpClient.DefaultRequestHeaders.Remove("Environment");
            _httpClient.DefaultRequestHeaders.Add("Environment", settings.Environment);

            _httpClient.DefaultRequestHeaders.Remove("AuthToken");
            _httpClient.DefaultRequestHeaders.Add("AuthToken", settings.AuthToken);

            _httpClient.DefaultRequestHeaders.Remove("Secret");
            _httpClient.DefaultRequestHeaders.Add("Secret", settings.Secret);

            _httpClient.DefaultRequestHeaders.Remove("AppID");
            _httpClient.DefaultRequestHeaders.Add("AppID", settings.AppID.ToString());

            var httpContent = new MultipartFormDataContent
        {
            { new StringContent(JsonConvert.SerializeObject(request.SearchCriteria)), "SearchCriteria" },
            { new StringContent(settings.AppID.ToString()), "AppID" },
            { new StringContent(JsonConvert.SerializeObject(settings.secretContent.ToString())), "secretContent" }
        };

            var response = await _httpClient.PostAsync(settings.BaseUrl, httpContent);
            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<CustomerStatementResponse>(json);
        }
        private async Task<FinancialSettingsDb?> GetFinancialIntegrationSettings()
        {
            var settings = await appsettingsRepository.Query().AsNoTracking().Where(x => x.Id=="FinancialIntegrationSettings").FirstOrDefaultAsync();
            if (settings!=null)
            {
                return JsonConvert.DeserializeObject<FinancialSettingsDb>(settings.Value);
            }

            return null;
        }
    }
}