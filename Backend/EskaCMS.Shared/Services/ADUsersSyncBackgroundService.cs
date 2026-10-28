//using EskaCMS.ADIntegration.Interfaces;
//using EskaCMS.ADIntegration.ViewModels;
//using EskaCMS.Core.Areas.Core.ViewModels.Account;
//using EskaCMS.Core.BusinessModels;
//using EskaCMS.Core.Models;
//using EskaCMS.Core.Services;
//using EskaCMS.Core.ViewModels;
//using EskaCMS.Entities.Entity;
//using EskaCMS.Entities.Entity.Core;


//using EskaCMS.Infrastructure.Data;
//using EskaCMS.Security.Models;
//using EskaCMS.Security.Services.Interfaces;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using OfficeOpenXml.Packaging.Ionic.Zlib;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Design;
//using System.Linq;
//using System.Net;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;
//using static EskaCMS.Entities.Enum.GeneralEnums;

//namespace EskaCMS.ADIntegration.Services
//{
//    public class ADUsersSyncBackgroundService : BackgroundService
//    {
//        private  long SystemUserId = 2;
//        private readonly IServiceProvider _serviceProvider;
//        private readonly IEskaCoreIntegrationService _eskaCoreIntegrationService;
//        private readonly ILogger _logger;

//        public readonly IConfiguration _configuration;
//        public string ADServiceExecutionTime = "0";
//        public string BaseActiveDirectoryUrl = "";
//        public string ExecutionPerEmployee = "0";
//        public string AdUsername = "Y.Hindi";
//        public string AdUserPassword = "Eska123";
//        public string BaseHrSystemUrl = "";
//        public long DefaultSiteId = 1;
//        public string BranchId = "";
//        public string CompanyId = "";
//        public ADUsersSyncBackgroundService(
//            IServiceProvider serviceProvider,
//                        IEskaCoreIntegrationService eskaCoreIntegrationService,

//            ILogger<ADUsersSyncBackgroundService> logger,
//            IConfiguration configuration)

//        {
//            _serviceProvider = serviceProvider;
//            _eskaCoreIntegrationService = eskaCoreIntegrationService;
//            _logger = logger;
//            _configuration = configuration;
//            ADServiceExecutionTime = _configuration.GetValue<string>("ActiveDirectory:ADServiceExecutionTimeInMinutes");
//            BaseActiveDirectoryUrl = _configuration.GetValue<string>("ActiveDirectory:ADServiceExecutionTimeInMinutes");
//            ExecutionPerEmployee = _configuration.GetValue<string>("ActiveDirectory:ExecutionPerEmployeeInSeconds");

//            AdUsername = _configuration.GetValue<string>("ActiveDirectory:AdUsername");
//            AdUserPassword = _configuration.GetValue<string>("ActiveDirectory:AdUserPassword");
//            BaseHrSystemUrl = _configuration.GetValue<string>("ActiveDirectory:BaseHrSystemUrl");
//            DefaultSiteId = _configuration.GetValue<long>("ActiveDirectory:DefaultSiteId");
//            SystemUserId = _configuration.GetValue<long>("ActiveDirectory:SystemUserId");
//            BranchId = _configuration.GetValue<string>("EskaCore:BranchId");
//            CompanyId = _configuration.GetValue<string>("EskaCore:CompanyId");
//        }

//        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            try
//            {


//                _logger.LogInformation("AD Users Sync Background Service is starting.");
//                while (!stoppingToken.IsCancellationRequested)
//                {

//                    _logger.LogInformation("AD Users Sync Background Service is working.");
//                    await Task.Delay(TimeSpan.FromMinutes(double.Parse(ADServiceExecutionTime)), stoppingToken);

//                    using (var scope = _serviceProvider.CreateScope())
//                    {
//                        List<ActiveDirectoryViewModel> adUsers = await GetActiveDirectoryUsers();
//                        IRepository<CustomerGroup> groupRepository = scope.ServiceProvider.GetRequiredService<IRepository<CustomerGroup>>();
//                        IRepository<UsersSites> userSiteRepository = scope.ServiceProvider.GetRequiredService<IRepository<UsersSites>>();
//                        IRepository<User> userRepository = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
//                        await SyncActiveDirectoryUsers(adUsers, groupRepository, userSiteRepository, userRepository, stoppingToken);
//                        _logger.LogInformation("AD Users Sync Background Service is finalized SyncActiveDirectoryUsers.");
//                    }
//                }
//            }
//            catch (Exception exc)
//            {
//                _logger.LogInformation("AD Users Sync Background Service Error : " + exc.Message);
//                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
//                //throw;
//            }
//        }

//        private async Task<List<ActiveDirectoryViewModel>> GetActiveDirectoryUsers()
//        {
//            try
//            {
//                var credentialsCache = new NetworkCredential(AdUsername, AdUserPassword);
//                var handler = new HttpClientHandler { Credentials = credentialsCache };
//                using (var client = new HttpClient(handler))
//                {
//                    client.BaseAddress = new Uri(BaseActiveDirectoryUrl);
//                    client.DefaultRequestHeaders.Clear();
//                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
//                    //GET Method  
//                    HttpResponseMessage response = await client.GetAsync("api/Identity/GetDomainUsers");
//                    string responseContent = response.Content.ReadAsStringAsync().Result;
//                    EskaCoreBaseResponseVM<List<ActiveDirectoryViewModel>> returnResponse = JsonSerializer.Deserialize<EskaCoreBaseResponseVM<List<ActiveDirectoryViewModel>>>(responseContent);
//                    return returnResponse.content;
//                }
//            }
//            catch (Exception e)
//            {

//                throw e;
//            }

//        }
//        private async Task SyncActiveDirectoryUsers(List<ActiveDirectoryViewModel> adUsers,
//            IRepository<CustomerGroup> groupRepository,
//            IRepository<UsersSites> userSitesRepository, 
//            IRepository<User> userRepository,
//            CancellationToken stoppingToken)
//        {

//            foreach (var adUser in adUsers)
//            {
//                try
//                {

//                    User user = userRepository.Query().Include(x => x.UserRoles).Include(u => u.UserSites).Where(x => x.UserName.ToLower() == adUser.samAccountName.ToLower()).FirstOrDefault();
//                    HREmployee employee = await GetEmployee(adUser.samAccountName);
//                    if (employee != null)
//                    {
//                        adUser.voiceTelephoneNumber = employee.MOBILE;
//                    }
//                    else
//                    {
//                        Random generator = new Random();
//                        String r = generator.Next(0, 1000000).ToString("D6");
//                        adUser.voiceTelephoneNumber = "0781" + r;
//                    }
//                    _logger.LogInformation("AD Users Sync Background Service : Get Eska Core Token for user : " + adUser.samAccountName);
//                    string token = _eskaCoreIntegrationService.Authenticate().Result.content.Token;
//                    _logger.LogInformation("AD Users Sync Background Service : Eska Core Token : " + token);
//                    if (user == null)
//                    {
//                        _logger.LogInformation("AD Users Sync Background Service : Create DCMS User : " + adUser.samAccountName);
//                        await CreateUser(adUser, groupRepository, userSitesRepository, userRepository, token);
//                        _logger.LogInformation("AD Users Sync Background Service : Successful User Creation for User : " + adUser.samAccountName);
//                    }
//                    else
//                    {
//                        _logger.LogInformation("AD Users Sync Background Service : User is already exist in DCMS DB : " + user.UserName);
//                        List<HREmployee> employees = await GetEmployees(adUser.samAccountName);
//                        _logger.LogInformation("AD Users Sync Background Service : Get Employee from HR  : " + user.UserName);
//                        if (user.UserRoles.Count <= 0)
//                        {
//                            user.UserRoles = AddUserRoles(6);
//                            userRepository.SaveChanges();
//                        }

//                        foreach (var empl in employees)
//                        {
//                            if (!string.IsNullOrEmpty(empl.USERNAME))
//                            {
//                                User emp = userRepository.Query().Where(x => x.UserName == empl.USERNAME).FirstOrDefault();
//                                if (emp != null)

//                                {
//                                    emp.ParentId = user.Id;
//                                    userRepository.SaveChanges();
//                                }


//                            }
//                        }
//                    }
//                }
//                catch (Exception e)
//                {
//                    _logger.LogInformation("SyncActiveDirectoryUsers : Error   : " + e.Message);
                  
//                }
//            }

//        }
//        private async Task<List<HREmployee>> GetEmployees(string username)
//        {
//            using (var client = new HttpClient())
//            {
//                List<HREmployee> respone = new List<HREmployee>();
//                client.BaseAddress = new Uri(BaseHrSystemUrl);
//                client.DefaultRequestHeaders.Accept.Clear();
//                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
//                //GET Method  
//                HttpResponseMessage response = await client.GetAsync($"GetEmployees?CompanyId={CompanyId}&PageNumber=1&RowsCountPerPage=&EmployeeName=&EmployeeNo=&nBranch={BranchId}&nDepartment=&EmployeeStatus=2");
//                string responseContent = response.Content.ReadAsStringAsync().Result;
//                CoreResponseViewModel<List<HREmployee>> returnResponse = JsonSerializer.Deserialize<CoreResponseViewModel<List<HREmployee>>>(responseContent);
//                HREmployee employee = returnResponse.Data.Where(x => x.USERNAME == username).FirstOrDefault();
//                if (employee != null)
//                {
//                    List<HREmployee> employees = returnResponse.Data.Where(x => x.MANAGER_ID == employee.ID).ToList();
//                    return employees;
//                }
//                return respone;
//            }

//        }
//        private async Task<HREmployee> GetEmployee(string username)
//        {
//            using (var client = new HttpClient())
//            {
//                List<HREmployee> respone = new List<HREmployee>();
//                client.BaseAddress = new Uri(BaseHrSystemUrl);
//                client.DefaultRequestHeaders.Accept.Clear();
//                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
//                //GET Method  
//                HttpResponseMessage response = await client.GetAsync($"GetEmployees?CompanyId={CompanyId}&PageNumber=1&RowsCountPerPage=&EmployeeName=&EmployeeNo=&nBranch={BranchId}&nDepartment=&EmployeeStatus=2");
//                string responseContent = response.Content.ReadAsStringAsync().Result;
//                CoreResponseViewModel<List<HREmployee>> returnResponse = JsonSerializer.Deserialize<CoreResponseViewModel<List<HREmployee>>>(responseContent);
//                HREmployee employee = returnResponse.Data.Where(x => x.USERNAME == username).FirstOrDefault();
//                return employee;
//            }

//        }
//        private async Task CreateUser(ActiveDirectoryViewModel adUser, IRepository<CustomerGroup> groupRepository, IRepository<UsersSites> userSitesRepository, IRepository<User> userRepository, string token)
//        {


//            try
//            {
//                User user = new User
//                {
//                    UserName = adUser.samAccountName,
//                    Email = adUser.emailAddress,
//                    FullName = adUser.displayName,
//                    Type = EUsersTypes.Customer,
//                    CreatedById = SystemUserId,
//                    IsADAuthRequired = true,
//                    PhoneNumber = adUser.voiceTelephoneNumber,
//                    Status = EStatus.Active,
//                    ConcurrencyStamp = "05286794-6c65-4c3e-8de2-e6a10b85d050",
//                    PasswordHash = "AQAAAAEAACcQAAAAELIGzYdxN6Giv7M0KV3bOlPZanDbaOH+u71HsLZp1d8sNf0lOhnP+Kb4zRt7C1h7Dw==",
//                    PhoneNumberConfirmed = false,
//                    SecurityStamp = "AQAAAAEAACcQAAAAELIGzYdxN6Giv7M0KV3bOlPZanDbaOH+u71HsLZp1d8sNf0lOhnP+Kb4zRt7C1h7Dw=="
//                };
//                string adUserDepartment = adUser.distinguishedName.Split(',')[1];
//                string department = adUserDepartment.Split('=')[1];
//                CustomerGroup customerGroup = groupRepository.Query().Where(x => x.Name == department).FirstOrDefault();
//                if (customerGroup == null)
//                {
//                    long groupId = CreateGroup(department, groupRepository);
//                    user.CustomerGroups = AddUserGroups(groupId);
//                }
//                else
//                {
//                    user.CustomerGroups = AddUserGroups(customerGroup.Id);
//                }
//                user.UserRoles = AddUserRoles(6);
//                userRepository.Add(user);
//                userRepository.SaveChanges();
//                UsersSites UserToSite = new UsersSites { UserId = user.Id, SiteId = DefaultSiteId, IsDefault = true };
//                userSitesRepository.Add(UserToSite);
//                userSitesRepository.SaveChanges();
//                Site site = userSitesRepository.Query().Include(x => x.Site).Where(x => x.SiteId == DefaultSiteId).Select(x => x.Site).FirstOrDefault();

//                 EskaCoreBaseResponseViewModel<List<CoreGroupViewModel>> userInfo = await _eskaCoreIntegrationService.GetUserInfo("", user.UserName);
//                    if (userInfo.content.Count == 0)
//                    {
//                        UserForm userForm = new UserForm
//                        {
//                            Email = adUser.emailAddress,
//                            FullName = adUser.displayName,
//                            UserName = adUser.samAccountName,
//                            Password = "Eska@123"
//                        };

//                        await _eskaCoreIntegrationService.InsertUser(userForm, token);
//                    }
                
//            }
//            catch (Exception e)
//            {

//                throw e;
//            }
//        }
//        private static List<CustomerGroupUser> AddUserGroups(long groupId)
//        {
//            List<CustomerGroupUser> userGroups = new List<CustomerGroupUser>();
//            CustomerGroupUser userCustomerGroup = new CustomerGroupUser
//            {
//                CustomerGroupId = groupId
//            };

//            userGroups.Add(userCustomerGroup);
//            return userGroups;
//        }
//        private static List<UserRole> AddUserRoles(long roleId)
//        {
//            List<UserRole> userRoles = new List<UserRole>();
//            UserRole userRole = new UserRole
//            {
//                RoleId = roleId
//            };
//            userRoles.Add(userRole);
//            return userRoles;
//        }
//        private long CreateGroup(string name, IRepository<CustomerGroup> groupRepository)
//        {
//            CustomerGroup group = new CustomerGroup
//            {
//                CreatedOn = DateTime.Now,
//                Description = name,
//                IsActive = false,
//                IsDeleted = false,
//                LatestUpdatedOn = DateTime.Now,
//                Name = name,
//                SiteId = DefaultSiteId,
//                FireBaseTopic = "ADDepartment"
//            };
//            groupRepository.Add(group);
//            groupRepository.SaveChanges();

//            return group.Id;
//        }
//    }

//}
