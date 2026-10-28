
using EskaDCMSActiveDirectory.Extensions;
using EskaDCMSActiveDirectory.Interfaces;
using EskaDCMSActiveDirectory.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EskaDCMSActiveDirectory.Services
{
    public class AdUserProvider : IAdUserProvider
    {
        public ActiveDirectoryUser CurrentUser { get; set; }
        public bool Initialized { get; set; }
        public string Url = "";
        public int BranchId = 1;
        public int CompanyId = 1;
        public int PageNumber = 1;

        public int EmployeeStatus = 2;
        private HttpContext _httpContext;

        private readonly IServiceProvider serviceProvider;
        private readonly IHttpContextAccessor contextAccessor;
        private readonly IConfiguration configuration;
        private readonly System.Net.Http.IHttpClientFactory httpClientFactory;

        public AdUserProvider(IServiceProvider serviceProvider,
            IHttpContextAccessor contextAccessor

, IConfiguration configuration, System.Net.Http.IHttpClientFactory httpClientFactory)
        {
            this.serviceProvider = serviceProvider;
            this.contextAccessor = contextAccessor;
            this.configuration = configuration;
            this.httpClientFactory = httpClientFactory;
            _httpContext = contextAccessor.HttpContext;
        }

        public async Task Create(HttpContext context, IConfiguration config)
        {
            Url = config.GetSection("ActiveDirectoryConfig:Url").Value;
            BranchId = Convert.ToInt32(config.GetSection("ActiveDirectoryConfig:BranchId").Value);
            CompanyId = Convert.ToInt32(config.GetSection("ActiveDirectoryConfig:CompanyId").Value);
            EmployeeStatus = Convert.ToInt32(config.GetSection("ActiveDirectoryConfig:EmployeeStatus").Value);
            PageNumber = Convert.ToInt32(config.GetSection("ActiveDirectoryConfig:PageNumber").Value);
            CurrentUser = await GetAdUser(context.User.Identity);

            Initialized = true;
        }

        public Task<ActiveDirectoryUser> GetAdUser(IIdentity identity)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var context = new PrincipalContext(ContextType.Domain);
                    var principal = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, identity.Name);

                    if (principal == null)
                        throw new Exception($"User '{identity.Name}' not found in Active Directory.");

                    return ActiveDirectoryUser.CastToAdUser(principal, "");
                }
                catch (Exception ex)
                {
                    throw new Exception("Error retrieving AD User", ex);
                }
            });
        }

        public async Task<string> GetManagerName(string username)
        {

            using (var client = httpClientFactory.CreateClient())
            {
                client.BaseAddress = new Uri(Url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                //GET Method  
                HttpResponseMessage response = await client.GetAsync($"GetEmployees?CompanyId={CompanyId}&PageNumber={PageNumber}&RowsCountPerPage=&EmployeeName=&EmployeeNo=&nBranch={BranchId}&nDepartment=&EmployeeStatus={EmployeeStatus}");
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    CoreResponse<List<HREmployee>> returnResponse = JsonConvert.DeserializeObject<CoreResponse<List<HREmployee>>>(responseContent);
                    HREmployee employee = returnResponse.Data.Where(x => (x.USERNAME != null) && (x.USERNAME.ToUpper() == username.ToUpper())).FirstOrDefault();
                    if (employee == null)
                    {
                        throw new Exception("User Not Found In HR System");
                    }
                    HREmployee manager = returnResponse.Data.Where(x => x.ID == employee.MANAGER_ID).FirstOrDefault();
                    if (manager != null)
                    {
                        return manager.NAME;
                    }
                    return "";
                }
                else
                {
                    return "";
                }
            }



        }
        public Task<ActiveDirectoryUser> GetAdUser(string samAccountName)
        {
            return Task.Run(() =>
            {
                try
                {
                    PrincipalContext context = new PrincipalContext(ContextType.Domain);
                    UserPrincipal principal = new UserPrincipal(context);

                    if (context != null)
                    {
                        principal = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, samAccountName);
                    }

                    return ActiveDirectoryUser.CastToAdUser(principal, null);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error retrieving AD User", ex);
                }
            });
        }

        public Task<ActiveDirectoryUser> GetAdUser(Guid guid)
        {
            return Task.Run(() =>
            {
                try
                {
                    PrincipalContext context = new PrincipalContext(ContextType.Domain);
                    UserPrincipal principal = new UserPrincipal(context);

                    if (context != null)
                    {
                        principal = UserPrincipal.FindByIdentity(context, IdentityType.Guid, guid.ToString());
                    }

                    return ActiveDirectoryUser.CastToAdUser(principal, "");
                }
                catch (Exception ex)
                {
                    throw new Exception("Error retrieving AD User", ex);
                }
            });
        }

        public async Task<List<ActiveDirectoryUser>> GetDomainUsers()
        {

            PrincipalContext context = new PrincipalContext(ContextType.Domain);
            UserPrincipal principal = new UserPrincipal(context);
            principal.UserPrincipalName = "*@*";
            principal.Enabled = true;
            PrincipalSearcher searcher = new PrincipalSearcher(principal);

            var users = searcher
            .FindAll()
            .AsQueryable()
            .Cast<UserPrincipal>()
            .FilterUsers()
            .Select(x => new ActiveDirectoryUser
            {
                samAccountName = x.SamAccountName,
                description = x.Description,
                sID = x.Sid,
                displayName = x.DisplayName,
                emailAddress = x.EmailAddress,
                voiceTelephoneNumber = x.VoiceTelephoneNumber,
                surname = x.Surname,
                DistinguishedName = x.DistinguishedName
            })
            .OrderBy(x => x.surname)
            .ToList();

            return users;

        }

        public Task<List<ActiveDirectoryUser>> FindDomainUser(string search)
        {
            return Task.Run(() =>
            {
                PrincipalContext context = new PrincipalContext(ContextType.Domain);
                UserPrincipal principal = new UserPrincipal(context);
                principal.SamAccountName = $"*{search}*";
                principal.Enabled = true;
                PrincipalSearcher searcher = new PrincipalSearcher(principal);

                var users = searcher
                    .FindAll()
                    .AsQueryable()
                    .Cast<UserPrincipal>()
                    .FilterUsers()
                    .SelectAdUsers()
                    .OrderBy(x => x.surname)
                    .ToList();

                return users;
            });
        }

        public async Task<LoginWithADVM> Login(ActiveDirectoryUser CurrentUser, long siteId)
        {
            try
            {

                bool UseCoreIntegration = configuration.GetValue<bool>("CoreAPIs:UseCoreIntegration");

                var Result =  await DCMSLogin(CurrentUser, siteId);

                if (UseCoreIntegration)
                {
                    Result.CoreToken = await CORELogin(CurrentUser);
                }
                 await SetAuthCookies(Result.LoginResponse.Token);

               
                return Result;

            }
            catch (Exception)
            {

                throw;
            }
        }
        private async Task<LoginWithADVM> DCMSLogin(ActiveDirectoryUser CurrentUser, long siteId)
        {

            using (HttpClient client = httpClientFactory.CreateClient())
            {
                string url = configuration.GetValue<string>("DCMSAPIs:BaseURL") + configuration.GetValue<string>("DCMSAPIs:LoginAPI");

                LoginViewModel payload = new LoginViewModel(CurrentUser.samAccountName, CurrentUser.sID.Value);
                var content = new StringContent(payload.SerializeObject(), Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + configuration.GetValue<string>("DCMSAPIs:LongLifeToken"));
                client.DefaultRequestHeaders.Add("siteId", siteId.ToString());
                client.DefaultRequestHeaders.Add("IntegrationPermission", configuration.GetValue<string>("DCMSAPIs:IntegrationPermission"));


                HttpResponseMessage response = await client.PostAsync(url, content);

                
                string responseBody = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    DCMSResponse<LoginResponse> Result = JsonConvert.DeserializeObject<DCMSResponse<LoginResponse>>(responseBody);                    
                    return new LoginWithADVM(CurrentUser, Result);
                }
                else
                {
                    DCMSResponse<string> Result = JsonConvert.DeserializeObject<DCMSResponse<string>>(responseBody);
                    if (Result is null || string.IsNullOrEmpty(Result.message))
                    {
                        throw new Exception(CurrentUser.samAccountName);

                    }
                    throw new Exception(Result.message);
                }
            }
        }
        public async Task AppendHttpOnlyCookie(string key, string value, int expiryInYears)
        {
            try
            {
                var context = contextAccessor.HttpContext;
                var request = context?.Request;

                if (context == null || request == null)
                    return;

                var currentHost = request.Headers["Origin"].ToString();
                if (string.IsNullOrWhiteSpace(currentHost))
                {
                    var protocol = request.IsHttps ? "https://" : "http://";
                    currentHost = protocol + request.Host.Value;
                }

                var fullHost = currentHost.Replace("http://", "").Replace("https://", "");
                var host = fullHost.Contains(":") ? fullHost.Split(':')[0] : fullHost;

                var isSecure = currentHost.StartsWith("https", StringComparison.OrdinalIgnoreCase);


                context.Response.Cookies.Delete(key);
                context.Response.Cookies.Delete(key, new CookieOptions
                {
                    Domain = request.Host.Host,
                    Path = "/"
                });
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // HTTPS only if secure
                    Expires = DateTimeOffset.UtcNow.AddYears(expiryInYears),
                    SameSite = SameSiteMode.Lax, 
                    Path = "/",
                    IsEssential = true
                    ,
                    Domain= ".eskadenia.com"
                };

                //if (!host.Contains("localhost") && !IPAddress.TryParse(host, out _))
                //{
                //    var domainParts = host.Split('.');
                //    if (domainParts.Length >= 2)
                //    {
                //        cookieOptions.Domain = "." + string.Join('.', domainParts.TakeLast(2));
                //    }
                //}

                context.Response.Cookies.Delete(key);
                context.Response.Cookies.Append(key, value, cookieOptions);
            }
            catch (Exception ex)
            {
                // Optional logging
                throw;
            }
        }


        public async Task AppendHttpOnlyCookie(string key, string value)
        {



            await AppendHttpOnlyCookie(key, value, 1);
        }
        private async Task SetAuthCookies(TokenResponse Token)
        {

            await AppendHttpOnlyCookie("web_access_token", Token.Token);
            await AppendHttpOnlyCookie("web_refresh_token", Token.RefreshToken);

        }
        private async Task<CoreLoginResponse> CORELogin(ActiveDirectoryUser CurrentUser)
        {

            using (HttpClient client = httpClientFactory.CreateClient())
            {

                string url = configuration.GetValue<string>("CoreAPIs:BaseURL") + configuration.GetValue<string>("CoreAPIs:LoginWithoutPassword");

                CoreLoginViewModel payload = new CoreLoginViewModel(CurrentUser.samAccountName);
                var content = new StringContent(payload.SerializeObject(), Encoding.UTF8, "application/json");


                HttpResponseMessage response = await client.PostAsync(url, content);

                string responseBody = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    DCMSResponse<CoreLoginResponse> Result = JsonConvert.DeserializeObject<DCMSResponse<CoreLoginResponse>>(responseBody);
                    return Result.content;
                }
                else
                {
                    DCMSResponse<string> Result = JsonConvert.DeserializeObject<DCMSResponse<string>>(responseBody);
                    if (Result is null || string.IsNullOrEmpty(Result.message))
                    {
                        throw new Exception("Unable to Login to Core");

                    }
                    throw new Exception(Result.message);
                }
            }
        }

        public async Task<CoreLoginResponse> CoreRefreshToken(RefreshCoreTokenVM model)
        {

            using (HttpClient client = httpClientFactory.CreateClient())
            {

                string url = configuration.GetValue<string>("CoreAPIs:BaseURL") + configuration.GetValue<string>("CoreAPIs:RefreshToken");
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");


                HttpResponseMessage response = await client.PostAsync(url, content);

                string responseBody = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    DCMSResponse<CoreLoginResponse> Result = JsonConvert.DeserializeObject<DCMSResponse<CoreLoginResponse>>(responseBody);
                    Result.content.SessionId = model.SessionId;
                    return Result.content;
                }
                else
                {
                    DCMSResponse<string> Result = JsonConvert.DeserializeObject<DCMSResponse<string>>(responseBody);
                    if (Result is null || string.IsNullOrEmpty(Result.message))
                    {
                        throw new Exception("Unable to Login to Core");

                    }
                    throw new Exception(Result.message);
                }
            }
        }

        public async Task<bool> ValidateAd(loginRequestViewModel model)
        {
            try
            {
                await Task.CompletedTask;
                string domainName = System.Environment.UserDomainName;
                string domainUserName = System.Environment.UserName;
                PrincipalContext pc = new PrincipalContext(ContextType.Domain, domainName, domainUserName, ContextOptions.SimpleBind.ToString());
                bool isValid = pc.ValidateCredentials(model.Username.ToUpper(), model.Password);
                return isValid;
            
            }
            catch (Exception)
            {

                return false;
            }
        }
    }

}
