using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using System.Text.Json;
using EskaCMS.Engine.Models;
using EskaCMS.Engine.Services.Interfaces;
using EskaCMS.Entities.Entity.Core;
using Newtonsoft.Json;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Infrastructure.Extensions;
using OfficeOpenXml;
using Elastic.Clients.Elasticsearch;

namespace EskaCMS.Engine.BGS
{
    public class ADUsersSyncBackgroundService : BaseBackgroundService
    {
        private long SystemUserId = 2;
        private readonly ILogger<ADUsersSyncBackgroundService> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IConfiguration configuration;
        public long ADServiceExecutionTime = 0;
        public string BaseActiveDirectoryUrl = "";
        public string ExecutionPerEmployee = "0";
        public string AdUsername = "Y.Hindi";
        public string AdUserPassword = "Eska321";
        public string AdDomain = "atheeb";
        public string BaseHrSystemUrl = "";
        public long DefaultSiteId = 27;
        public long RoleType = 77;
        public string BranchId = "";
        public string CompanyId = "";
        private string DefaultConnection = "";
        private string FileBaseUrl = "";

        public ADUsersSyncBackgroundService(ILogger<ADUsersSyncBackgroundService> logger,
            System.IServiceProvider serviceProvider,
            IConfiguration Configuration) : base(nameof(ADUsersSyncBackgroundService), logger, serviceProvider, Configuration)

        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            configuration = Configuration;
            DefaultConnection = configuration.GetValue<string>("ConnectionStrings:DefaultConnection");

            ADServiceExecutionTime = Convert.ToInt32(configuration.GetSection("ActiveDirectory:ADServiceExecutionTimeInMinutes").Value);
            BaseActiveDirectoryUrl = configuration.GetValue<string>("ActiveDirectory:ActiveDirectoryUrl");
            ExecutionPerEmployee = configuration.GetValue<string>("ActiveDirectory:ExecutionPerEmployeeInSeconds");
            AdUsername = configuration.GetValue<string>("ActiveDirectory:AdUsername");
            AdUserPassword = configuration.GetValue<string>("ActiveDirectory:AdUserPassword");
            AdDomain = configuration.GetValue<string>("ActiveDirectory:AdDomain");
            BaseHrSystemUrl = configuration.GetValue<string>("ActiveDirectory:BaseHrSystemUrl");
            DefaultSiteId = configuration.GetValue<long>("ActiveDirectory:DefaultSiteId");
            RoleType = configuration.GetValue<long>("ActiveDirectory:RoleType");
            SystemUserId = configuration.GetValue<long>("ActiveDirectory:SystemUserId");
            BranchId = configuration.GetValue<string>("EskaCore:BranchId");
            CompanyId = configuration.GetValue<string>("EskaCore:CompanyId");
            FileBaseUrl = configuration.GetValue<string>("ActiveDirectory:FileBaseUrl");


        }
        public override async Task<bool> Execute()
        {
            try
            {
                TimeSpan period = TimeSpan.FromSeconds(ADServiceExecutionTime);
                using PeriodicTimer timer = new PeriodicTimer(period);
                DateTimeOffset? lastExecutionTime = null;

                logger.LogInformation("AD Users Sync Background Service is working.");
                using (var scope = serviceProvider.CreateScope())
                {
                    List<ActiveDirectoryViewModel> adUsers = await GetActiveDirectoryUsers();
                    logger.LogInformation("AD Users Sync Background Service is finalized SyncActiveDirectoryUsers.");
                    await SyncActiveDirectoryUsers(adUsers, scope);
                }
                return true;

            }
            catch (Exception ex)
            {
                logger.LogInformation($"AD Users Sync Background Service is finalized SyncActiveDirectoryUsers {ex.Message}.");
                return false;

            }
        }
        private async Task<List<ActiveDirectoryViewModel>> GetActiveDirectoryUsers()
        {
            try
            {

                var credentialsCache = new NetworkCredential(AdUsername, AdUserPassword, AdDomain);
                var handler = new HttpClientHandler { Credentials = credentialsCache };
                using (var client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri(BaseActiveDirectoryUrl);
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                    HttpResponseMessage response = await client.GetAsync("adapi/Identity/GetDomainUsers");
                    logger.LogInformation($"AD Users Sync Background Service Start Read Data From GetActiveDirectoryUsers.");
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    EskaCoreBaseResponseVM<List<ActiveDirectoryViewModel>> returnResponse = System.Text.Json.JsonSerializer.Deserialize<EskaCoreBaseResponseVM<List<ActiveDirectoryViewModel>>>(responseContent);
                    return returnResponse.content;
                }
            }
            catch (Exception e)
            {
                logger.LogInformation($"AD Users Sync Background Service from GetActiveDirectoryUsers {e.Message}.");
                // throw e;
                return new List<ActiveDirectoryViewModel>();
            }
        }
        private async Task SyncActiveDirectoryUsers(List<ActiveDirectoryViewModel> adUsers, IServiceScope scope)
        {
            IActiveDirectoryServices ActiveDirectoryServices =
                scope.ServiceProvider.GetRequiredService<IActiveDirectoryServices>();

            // Never sync (and never delete) on an empty source: every DCMS user would look removed
            if (adUsers == null || !adUsers.Any())
            {
                logger.LogWarning("SyncActiveDirectoryUsers : AD returned no users, sync skipped.");
                return;
            }

            CoreResponseViewModel<List<HREmployee>>? returnResponse =
                await GetEmployee();

            if (returnResponse?.Data == null || !returnResponse.Data.Any() || returnResponse.IsError)
            {
                logger.LogWarning("SyncActiveDirectoryUsers : HR returned no employees, sync skipped. " + returnResponse?.ErrorMessage);
                return;
            }

            try
            {
                List<string> UserNames =
                    await ActiveDirectoryServices.GetAllUsernamesAsync();

                // =========================
                // Normalize usernames
                // =========================
                string Normalize(string? u) => u?.Trim().ToUpperInvariant() ?? "";

                var systemUsersSet = UserNames
                    .Select(Normalize)
                    .Where(x => x != "")
                    .ToHashSet();

                var hrActiveUsersSet = returnResponse.Data
                    .Where(x => x.STATUS != 3)
                    .Select(x => Normalize(x.USERNAME))
                    .Where(x => x != "")
                    .ToHashSet();

                // Status=2 returns active employees only, so the deleted ones (STATUS = 3) are requested separately
                var hrDeletedResponse = await GetEmployee("3");

                // STATUS = 3 is deleted in HR; a username that also has an active record (rehired) is kept
                var hrRemovedUsersSet = returnResponse.Data
                    .Concat(hrDeletedResponse?.Data ?? new List<HREmployee>())
                    .Where(x => x.STATUS == 3)
                    .Select(x => Normalize(x.USERNAME))
                    .Where(x => x != "" && !hrActiveUsersSet.Contains(x))
                    .ToHashSet();

                // DCMS usernames are HR usernames, so AD names are mapped the same way as in the sync loop below
                var adUsernamesSet = new HashSet<string>();
                // AD names that differ from the HR username but belong to an active HR employee
                var adNamesOfActiveHrSet = new HashSet<string>();
                foreach (var adName in adUsers.Select(u => Normalize(u.samAccountName)).Where(x => x != ""))
                {
                    adUsernamesSet.Add(adName);
                    if (!string.IsNullOrEmpty(FileBaseUrl) && !hrActiveUsersSet.Contains(adName))
                    {
                        var hrName = Normalize(FindHrSystemByAdName(adName));
                        if (hrName != "")
                        {
                            adUsernamesSet.Add(hrName);
                            if (hrActiveUsersSet.Contains(hrName))
                            {
                                adNamesOfActiveHrSet.Add(adName);
                            }
                        }
                    }
                }

                // =========================
                // Users removed in HR (STATUS = 3)
                // =========================
                var usersRemovedInHR = systemUsersSet
                    .Where(username => hrRemovedUsersSet.Contains(username))
                    .ToList();

                // =========================
                // Users still in AD but not active in HR (STATUS = 3 or not returned by HR at all)
                // (AD users only, so local admin/system accounts are never touched)
                // =========================
                var usersNotActiveInHR = systemUsersSet
                    .Where(username => adUsernamesSet.Contains(username)
                        && !hrActiveUsersSet.Contains(username)
                        && !adNamesOfActiveHrSet.Contains(username))
                    .ToList();

                // =========================
                // Users missing in AD
                // (HR employees only, so admin/system accounts are never touched)
                // =========================
                var usersRemovedInAD = systemUsersSet
                    .Where(username => hrActiveUsersSet.Contains(username) && !adUsernamesSet.Contains(username))
                    .ToList();

                // Many AD-only removals at once usually means a partial AD response: skip them, keep the rest of the sync
                if (usersRemovedInAD.Count > 20)
                {
                    logger.LogWarning($"SyncActiveDirectoryUsers : {usersRemovedInAD.Count} users missing in AD, AD deletions skipped this run : {string.Join(", ", usersRemovedInAD)}");
                    usersRemovedInAD.Clear();
                }

                // =========================
                // Execute deletion
                // =========================
                // A.ABUSALAMEH and B.YASOURI are maintained manually (skipped by the sync loop below), so never delete them
                var usersToDelete = usersRemovedInHR
                    .Union(usersNotActiveInHR)
                    .Union(usersRemovedInAD)
                    .Where(username => username != "IT.MANAGER" && username != "A.ABUSALAMEH" && username != "B.YASOURI")
                    .ToList();

                if (usersToDelete.Any())
                {
                    logger.LogInformation($"SyncActiveDirectoryUsers : Delete users. Removed in HR : {string.Join(", ", usersRemovedInHR)} | In AD but not active in HR : {string.Join(", ", usersNotActiveInHR)} | Missing in AD : {string.Join(", ", usersRemovedInAD)}");
                }
                foreach (var item in usersToDelete)
                {
                    await ActiveDirectoryServices.DeleteUserSP(item);
                }
            }
            catch (Exception e)
            {
                logger.LogInformation("SyncActiveDirectoryUsers : Delete Users Error : " + e.Message);
            }
            foreach (var adUser in adUsers)
            {
                try
                {
                    if (string.IsNullOrEmpty(adUser.samAccountName))
                    {
                        continue;
                    }
                    var adUserNameToUpper = adUser.samAccountName.ToUpper();
                    if ("A.ABUSALAMEH" != adUserNameToUpper && "B.YASOURI" != adUserNameToUpper)
                    {
                        // HR-deleted employees (STATUS = 3) are never created or updated
                        HREmployee employee = returnResponse.Data.Where(x => !string.IsNullOrEmpty(x.USERNAME)).Where(x => x.USERNAME.ToUpper() == adUserNameToUpper && x.STATUS!=3).FirstOrDefault();
                        if (employee == null)
                        {

                            var UserName = string.IsNullOrEmpty(FileBaseUrl) ? adUserNameToUpper : FindHrSystemByAdName(adUserNameToUpper);
                            if (UserName != null)
                            {
                                UserName = UserName.ToUpper();
                                employee = returnResponse.Data.Where(x => x.USERNAME?.ToUpper() == UserName && x.STATUS!=3).FirstOrDefault();
                            }
                        }
                        if (employee == null)
                        {
                            continue;
                        }
                        var user = await ActiveDirectoryServices.GetUserInfo(employee.USERNAME);
                        logger.LogInformation("employee :   : " + JsonConvert.SerializeObject(employee));

                        if (employee != null)
                        {
                            adUser.voiceTelephoneNumber = employee.MOBILE;
                        }
                        else
                        {
                            Random generator = new Random();
                            String r = new string(Enumerable.Repeat("0123456789", 8)
                                    .Select(s => s[generator.Next(s.Length)]).ToArray());
                            adUser.voiceTelephoneNumber = "05" + r;
                        }
                        if (user.Id == 0)
                        {
                            logger.LogInformation("AD Users Sync Background Service : Create DCMS User : " + adUser.samAccountName);
                            await ActiveDirectoryServices.CreateUser(adUser, employee);
                            logger.LogInformation("AD Users Sync Background Service : Successful User Creation for User : " + adUser.samAccountName);
                        }
                        else
                        {
 
                            try
                            {
                                logger.LogInformation("AD Users Sync Background Service : User is already exist in DCMS DB : " + user.UserName);
                                List<HREmployee> employees = returnResponse.Data.Where(x => x.MANAGER_ID == employee.ID).ToList();
                                logger.LogInformation("AD Users Sync Background Service : Get Employee from HR  : " + user.UserName);


                                var dept = new
                                {
                                    DepartmentId = employee.CRG_DPT_ID
                                };

                                string EnDepName = !string.IsNullOrEmpty(employee.DEPARTMENT_NAME) ? employee.DEPARTMENT_NAME : employee.DEPARTMENT_NAME2;
                                string ArDepName = !string.IsNullOrEmpty(employee.DEPARTMENT_NAME2) ? employee.DEPARTMENT_NAME2 : employee.DEPARTMENT_NAME;

                                var groupId = await ActiveDirectoryServices.CreateOrGetCustomerGroupAsync(EnDepName, ArDepName, System.Text.Json.JsonSerializer.Serialize(dept), DefaultConnection);
                                if (!string.IsNullOrEmpty(EnDepName))
                                {
                                    var RoleId = await ActiveDirectoryServices.CreateOrGetRoleAsync(EnDepName, RoleType, DefaultConnection);
                                    await ActiveDirectoryServices.AddUserRolesSP(Convert.ToInt32(user.Id), Convert.ToInt32(RoleId), RoleType);
                                }
                                await ActiveDirectoryServices.AddUserGroupSP(Convert.ToInt32(user.Id), Convert.ToInt32(groupId));
                                await ActiveDirectoryServices.AddUpdateUserOptionsSP(Convert.ToInt64(user.Id), "JOB_TITLE_NAME", employee.JOB_TITLE_NAME);
                                await ActiveDirectoryServices.AddUpdateUserOptionsSP(Convert.ToInt64(user.Id), "EmployeeId", employee.ID.ToString());
                                await ActiveDirectoryServices.AddUpdateUserOptionsSP(Convert.ToInt64(user.Id), "EMPLOYEE_RANK", employee.EMPLOYEE_RANK.ToString());
                                await ActiveDirectoryServices.AddUpdateUserOptionsSP(Convert.ToInt64(user.Id), "PHOTO_PATH", employee.PHOTO_PATH.ToString());
                                if (employee.PHONE2!=null)
                                {

                                    await ActiveDirectoryServices.AddUpdateUserOptionsSP(Convert.ToInt64(user.Id), "PHONE2", employee.PHONE2.ToString());
                                }
                                else
                                { 
                                }

                                if (employee.MANAGER_ID!=null)
                                {
                                    await ActiveDirectoryServices.AddUpdateUserOptionsSP(Convert.ToInt64(user.Id), "MANAGER_ID", employee.MANAGER_ID==null ? "" : employee.MANAGER_ID.ToString());

                                }
                                await ActiveDirectoryServices.AddUpdateUserOptionsSP(Convert.ToInt64(user.Id), "NAME2", employee.NAME2==null ? "" : employee.NAME2.ToString());
                                await ActiveDirectoryServices.AddUpdateUserOptionsSP(Convert.ToInt64(user.Id), "BIRTHDATE", employee.BIRTHDATE.ToString());
                                await ActiveDirectoryServices.AddUpdateUserOptionsSP(Convert.ToInt64(user.Id), "START_DATE", employee.START_DATE.ToString());
                                var FullName = GetFirstAndLastName(employee.NAME);

                                await ActiveDirectoryServices.UpdateUserGeneralInfo(Convert.ToInt64(user.Id), FullName, adUser.voiceTelephoneNumber, employee.COMPANY_EMAIL);
                                foreach (var empl in employees)
                                {
                                    if (!string.IsNullOrEmpty(empl.USERNAME))
                                    {
                                        await ActiveDirectoryServices.UpdateParentSP(empl?.USERNAME, Convert.ToInt32(user.Id));
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
                catch (Exception e)
                {
                    logger.LogInformation("SyncActiveDirectoryUsers : Error   : " + e.Message);
                }
            }

        }
        private static string GetFirstAndLastName(string? fullName)
        {
            var parts = fullName?
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return parts?.Length switch
            {
                null or 0 => string.Empty,
                1 => parts[0],
                _ => $"{parts[0]} {parts[^1]}"
            };
        }

        private async Task<CoreResponseViewModel<List<HREmployee>>> GetEmployee(string status = "2")
        {
            try
            {
                using (var client = serviceProvider.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient())
                {
                    List<HREmployee> respone = new List<HREmployee>();

                    client.BaseAddress = new Uri(BaseHrSystemUrl);

                    client.DefaultRequestHeaders.Accept.Clear();

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

                    HttpResponseMessage response = await client.GetAsync($"GetEmployees?CompanyId={CompanyId}&nPageNumber=1&nBranch={BranchId}&Status={status}");

                    string responseContent = response.Content.ReadAsStringAsync().Result;

                    CoreResponseViewModel<List<HREmployee>> returnResponse = System.Text.Json.JsonSerializer.Deserialize<CoreResponseViewModel<List<HREmployee>>>(responseContent);

                    return returnResponse;

                }
            }
            catch (Exception ex)
            {
                logger.LogInformation("GetEmployees : Error   : " + ex.Message);
                return null;
            }
        }
        public string FindHrSystemByAdName(string activeDirectoryName)
        {
            FileInfo fileInfo = new FileInfo(Path.Combine(FileBaseUrl, "AD_HR_ConflictNames.xlsx"));

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    string currentactiveDirectoryName = worksheet.Cells[row, 2].Text;


                    if (string.Equals(currentactiveDirectoryName, activeDirectoryName, StringComparison.OrdinalIgnoreCase))
                    {
                        return worksheet.Cells[row, 1].Text;
                    }

                }
            }

            return null;
        }
        private async Task<List<HREmployee>> GetEmployees(string username)
        {
            try
            {
                using (var client = serviceProvider.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient())
                {
                    List<HREmployee> respone = new List<HREmployee>();
                    client.BaseAddress = new Uri(BaseHrSystemUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                    HttpResponseMessage response = await client.GetAsync($"GetEmployees?CompanyId={CompanyId}&nPageNumber=1&nBranch={BranchId}&Status=2");
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    CoreResponseViewModel<List<HREmployee>> returnResponse = System.Text.Json.JsonSerializer.Deserialize<CoreResponseViewModel<List<HREmployee>>>(responseContent);
                    HREmployee employee = returnResponse.Data.Where(x => x.USERNAME.ToUpper() == username.ToUpper()).FirstOrDefault();
                    if (employee != null)
                    {
                        List<HREmployee> employees = returnResponse.Data.Where(x => x.MANAGER_ID == employee.ID).ToList();
                        return employees;
                    }
                    else
                    {
                        var UserName = string.IsNullOrEmpty(FileBaseUrl) ? username : FindHrSystemByAdName(username);
                        employee = returnResponse.Data.Where(b => !string.IsNullOrEmpty(b.USERNAME)).Where(x => x.USERNAME.ToUpper() == UserName.ToUpper()).FirstOrDefault();
                        if (employee != null)
                        {
                            List<HREmployee> employees = returnResponse.Data.Where(x => x.MANAGER_ID == employee.ID).ToList();
                            return employees;
                        }

                    }

                    return null;

                }
            }
            catch (Exception ex)
            {
                logger.LogInformation("GetEmployees : Error   : " + ex.Message);
                return null;
            }
        }
    }
}
