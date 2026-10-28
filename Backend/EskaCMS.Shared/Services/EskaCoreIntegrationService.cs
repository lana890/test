using EskaCMS.ADIntegration.Interfaces;
using EskaCMS.ADIntegration.ViewModels;
using EskaCMS.Core.BusinessModels;

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EskaCMS.ADIntegration.Services
{
    public class EskaCoreIntegrationService : IEskaCoreIntegrationService
    {
        public readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        public string baseApiUrl = "";
        public string BranchId = "";
        public string CompanyId = "";

        public EskaCoreIntegrationService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {

            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            baseApiUrl = _configuration.GetValue<string>("EskaCore:BaseApiUrl");
            BranchId = _configuration.GetValue<string>("EskaCore:BranchId");
            CompanyId = _configuration.GetValue<string>("EskaCore:CompanyId");
        }
    #region 
        public async Task<EskaCoreBaseResponseViewModel> InsertUser(UserForm user,string token="")
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                client.BaseAddress = new Uri(baseApiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",!string.IsNullOrEmpty(token)?token: Authenticate().Result.content.Token);

                //POST Method  
                EskaCoreInsertUserVM requestVM = new EskaCoreInsertUserVM
                {
                    BIRTH_DATE = DateTime.Now,
                    CREATED_BY = "ESKA DCMS",
                    CRG_BRN_ID = Convert.ToInt32( BranchId),
                    CRG_COM_ID = Convert.ToInt32(CompanyId),
                    EFFECTIVE_DATE = DateTime.Now.ToString(),
                    EMAIL = user.Email,
                    EXPIRY_DATE = DateTime.Now.AddYears(10).ToString(),
                    FAILED_LOGIN_ATTEMPTS = 0,
                    IP_ADDRESS = "1921684554",
                    NAME = user.FullName,
                    PASSWORD = user.Password,
                    PROF_ID = 1,
                    STATUS = 1,
                    STATUS_DATE = DateTime.Now.ToString(),
                    UI_THEME = 1,
                    USERNAME = user.UserName.ToUpper()
                };
                var request = JsonSerializer.Serialize(requestVM);
                var requestContent = new StringContent(request, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("api/v1/User/InsertUsers", requestContent);
                string responseContent = response.Content.ReadAsStringAsync().Result;
                EskaCoreBaseResponseViewModel returnResponse = JsonSerializer.Deserialize<EskaCoreBaseResponseViewModel>(responseContent);
                if (user.CoreGroupsIds != null)
                {
                    foreach (var group in user.CoreGroupsIds)
                    {
                        InsertCoreGroupVM insertCoreGroupVM = new InsertCoreGroupVM
                        {
                            CRG_COM_ID = Convert.ToInt32(CompanyId),
                            CSR_GRP_ID = group,
                            USERNAME = user.UserName.ToUpper()
                        };
                        
                        await InsertUserGroup(insertCoreGroupVM);
                    }
                }
                InsertUserBranchVM insertUserBranchVM = new InsertUserBranchVM
                {
                    CRG_COM_ID = Convert.ToInt32(CompanyId),
                    CRG_BRN_ID = Convert.ToInt32(BranchId),
                    Username = user.UserName.ToUpper(),
                    CREATED_BY="DCMS Background service"
                };
                await InsertUserBranch(insertUserBranchVM);
                return returnResponse;

            }
        }
        public async Task<EskaCoreBaseResponseViewModel> InsertUserGroup(InsertCoreGroupVM requestVM)
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                client.BaseAddress = new Uri(baseApiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Authenticate().Result.content.Token);

                //POST Method  

                var request = JsonSerializer.Serialize(requestVM);
                var requestContent = new StringContent(request, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("api/v1/Groups/InsertUserGroup", requestContent);
                string responseContent = response.Content.ReadAsStringAsync().Result;
                EskaCoreBaseResponseViewModel returnResponse = JsonSerializer.Deserialize<EskaCoreBaseResponseViewModel>(responseContent);
                return returnResponse;

            }

        } 
        public async Task<EskaCoreBaseResponseViewModel> InsertUserBranch(InsertUserBranchVM requestVM)
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                client.BaseAddress = new Uri(baseApiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", requestVM.AuthToken);

                //POST Method  

                var request = JsonSerializer.Serialize(requestVM);
                var requestContent = new StringContent(request, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("api/v1/Branch/InsertUserBranches", requestContent);
                string responseContent = response.Content.ReadAsStringAsync().Result;
                EskaCoreBaseResponseViewModel returnResponse = JsonSerializer.Deserialize<EskaCoreBaseResponseViewModel>(responseContent);
                return returnResponse;

            }

}
        public async Task<EskaCoreBaseResponseViewModel<List<CoreGroupViewModel>>> GetUserInfo(string authToken, string username)
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                client.BaseAddress = new Uri(baseApiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                //GET Method  
                HttpResponseMessage response = await client.GetAsync("api/v1/User/GetUsersInfo?USERNAME=" + username);
                if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    EskaCoreBaseResponseViewModel<List<CoreGroupViewModel>> returnResponse = JsonSerializer.Deserialize<EskaCoreBaseResponseViewModel<List<CoreGroupViewModel>>>(responseContent);
                    return returnResponse;
                }
                else
                {
                    EskaCoreBaseResponseViewModel<CoreAuthResponseVM> authResponse = await Authenticate();
                    return await GetUserInfo(authResponse.content.Token, username);
                }
            }

        }
        #endregion
        #region
        public async Task<EskaCoreBaseResponseViewModel<CoreAuthResponseVM>> Authenticate()
        {
            try
            {

                using (var client = _httpClientFactory.CreateClient())
                {
                    client.BaseAddress = new Uri(baseApiUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                    //POST Method

                   var CoreUser= _configuration.GetValue<string>("EskaCore:DefaultUserName");
                   var CorePassword= _configuration.GetValue<string>("EskaCore:DefaultPassword");
                    CoreAuthViewModel requestVM = new CoreAuthViewModel
                    {
                        BranchId = Convert.ToInt32(BranchId),
                        CompanyId = Convert.ToInt32(CompanyId),
                        Username = CoreUser,
                        Password = CorePassword,
                        IncludeRefreshToken = true
                    };
                    var request = JsonSerializer.Serialize(requestVM);
                    var requestContent = new StringContent(request, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("api/CoreAPIAuthenticator/Login", requestContent);
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    EskaCoreBaseResponseViewModel<CoreAuthResponseVM> returnResponse = JsonSerializer.Deserialize<EskaCoreBaseResponseViewModel<CoreAuthResponseVM>>(responseContent);
                    return returnResponse;

                }
            }
            catch (Exception e)
            {

                throw e;
            }

        }

        #endregion
       }
}
