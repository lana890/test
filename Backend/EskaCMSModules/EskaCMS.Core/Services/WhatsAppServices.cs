using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Enum;
using EskaCommerce.Module.Core.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Services
{
    public class WhatsAppServices : IWhatsAppServices
    {
        private readonly ISiteSettingsService settingservice;
        private readonly IHttpClientFactory _httpClientFactory;

        public WhatsAppServices(ISiteSettingsService settingservice, IHttpClientFactory httpClientFactory)
        {
            this.settingservice = settingservice;
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendMessageAsync(GeneralEnums.WhatsAppProviders WhatsAppProvider, string PhoneNumber,string ReciverName, bool IsPDF, string PdfLink, string Message, long SiteId, string AdditionInfo = "")
        {
            try
            {
                switch (WhatsAppProvider)
                {

                    case WhatsAppProviders.Infoseed:
                        await InfoseedSendAsync(PhoneNumber, ReciverName, IsPDF, PdfLink, Message, SiteId);

                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task InfoseedSendAsync(string phoneNumber, string ReciverName, bool IsPDF, string PdfLink, string message, long siteId)
        {
            try
            {

                var settings =await  settingservice.GetSiteSettingById(WhatsAppProviders.Infoseed.ToString(), siteId);



                if (settings == null)
                {
                    throw new Exception("The Provider not Exist");
                }

                var smsSetting = JsonConvert.DeserializeObject<InfoseedConfig>(settings.Value);

                // Prepare the request
                var request = new WhatsAppMessageRequest
                {
                    ReciverPhoneNumber = phoneNumber,
                    ReciverName = smsSetting.DefaultRecipientName ?? "Customer",
                    MssageContent = message,
                    IsPDF = IsPDF,
                    LinkPDF = IsPDF ? PdfLink : null
                };

                using (HttpClient httpClient = _httpClientFactory.CreateClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/plain"));
                    httpClient.DefaultRequestHeaders.Add("Tenancy", smsSetting.Tenancy);
                    httpClient.DefaultRequestHeaders.Add("Username", smsSetting.Username);
                    httpClient.DefaultRequestHeaders.Add("Password", smsSetting.Password);
                  httpClient.DefaultRequestHeaders.Add("TemplateName", smsSetting.TemplateName);
                    var jsonContent = JsonConvert.SerializeObject(request);
                    var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json-patch+json");

                    HttpResponseMessage response = await httpClient.PostAsync(smsSetting.ApiUrl, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Failed to send the message. Status code: {response.StatusCode} and Content: {responseContent}");
                    }
                    else {

                        return;
                    }

                }

            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
