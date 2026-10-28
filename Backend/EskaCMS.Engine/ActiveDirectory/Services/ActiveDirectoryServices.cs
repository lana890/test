using Elastic.Clients.Elasticsearch;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Engine.BGS;
using EskaCMS.Engine.Models;
using EskaCMS.Engine.Services.Interfaces;
using EskaCMS.Infrastructure.DbFactory;
using EskaCMS.Infrastructure.Enums;
using EskaCMS.Infrastructure.Helpers;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Engine.Services
{
    public class ActiveDirectoryServices : IActiveDirectoryServices
    {
        private long SystemUserId = 2;

        private readonly IConfiguration configuration;
        private readonly ILogger<ActiveDirectoryServices> logger;
        private readonly System.Net.Http.IHttpClientFactory _httpClientFactory;
        private string DefaultConnection = "";
        public string BaseHrSystemUrl = "";
        public long DefaultSiteId = 1;
        public string BranchId = "";
        public string CompanyId = "";
        public string baseApiUrl = "";
        public string CorePassword = "";
        public string CoreUser = "";
        public long SiteId = 27;
        public long RoleType = 77;
        public EProvider Provider;

        public ActiveDirectoryServices(IConfiguration configuration, ILogger<ActiveDirectoryServices> logger, System.Net.Http.IHttpClientFactory httpClientFactory)
        {
            this.configuration = configuration;
            this.logger = logger;
            _httpClientFactory = httpClientFactory;
            DefaultConnection = configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
            baseApiUrl = configuration.GetValue<string>("EskaCore:BaseApiUrl");
            BaseHrSystemUrl = configuration.GetValue<string>("ActiveDirectory:BaseHrSystemUrl");
            DefaultSiteId = configuration.GetValue<long>("ActiveDirectory:DefaultSiteId");
            RoleType = configuration.GetValue<long>("ActiveDirectory:RoleType");
            SystemUserId = configuration.GetValue<long>("ActiveDirectory:SystemUserId");
            BranchId = configuration.GetValue<string>("EskaCore:BranchId");
            CompanyId = configuration.GetValue<string>("EskaCore:CompanyId");
            CorePassword = configuration.GetValue<string>("EskaCore:DefaultPassword");
            CoreUser = configuration.GetValue<string>("EskaCore:DefaultUserName");
            SiteId = configuration.GetValue<long>("DCMSAPIs:SiteId");
            Provider = DbProviderHelper.ParseProvider(configuration.GetValue<string>("ConnectionStrings:DatabaseProvider"));
        }



        #region NpgsqlCommand And DCMS Apis
        public async Task<UserViewModel> GetUserInfo(string UserName)
        {
            try
            {
                var User = new UserViewModel();
                bool IsFirstRound = true;

                using (var connection = dbFactory.CreateConnection(DefaultConnection, Provider))
                {
                    using (var command = dbFactory.CreateCommand("GetUserDetailsByUsername", connection, Provider))
                    {
                        DbProviderHelper.ConfigureCommand(command, Provider);
                        command.CommandType = CommandType.StoredProcedure;
                        var paramUserName = command.CreateParameter();

                        paramUserName.ParameterName = DbProviderHelper.NormalizeParameterName(Provider, "@P_UserName");
                        paramUserName.Value = UserName;
                        paramUserName.DbType = DbType.String;
                        paramUserName.Direction = ParameterDirection.Input;
                        command.Parameters.Add(paramUserName);

                        await connection.OpenAsync();

                        using var reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            await fillUserDataFromSP(reader, User, IsFirstRound);


                        }

                    }

                }
                return User;
            }
            catch (Exception ex)
            {
                logger.LogInformation("GetUserInfo : Error   : " + ex.Message);
                return new UserViewModel();
            }
        }
        public async Task<long> CreateOrGetCustomerGroupAsync(string departmentEn, string departmentAr, string departmentId, string connectionString)
        {
            try
            {

                long empId = 0;
                var Cultures = await GetCurrentCulturesSP(Convert.ToInt32(SiteId));

                var CultureIdEn = Cultures.Where(x => x.CultureCode.Contains("en")).Select(x => x.CultureId).FirstOrDefault();
                var CultureIdAr = Cultures.Where(x => x.CultureCode.Contains("ar")).Select(x => x.CultureId).FirstOrDefault();

                using (var connection =dbFactory.CreateConnection(connectionString,Provider))
                {
                    using (var cmd =dbFactory.CreateCommand("CreateOrGetCustomerGroup",connection,Provider))
                    {
                        DbProviderHelper.ConfigureCommand(cmd, Provider);
                        cmd.CommandType = CommandType.StoredProcedure;

                        var paramDepartmentEn = cmd.CreateParameter();
                        paramDepartmentEn.ParameterName = DbProviderHelper.NormalizeParameterName(Provider, "@DepartmentEn");
                        paramDepartmentEn.Value = departmentEn;
                        paramDepartmentEn.DbType = DbType.String;
                        paramDepartmentEn.Direction = ParameterDirection.Input;
                        cmd.Parameters.Add(paramDepartmentEn);

                        var paramDepartmentCultureEn = cmd.CreateParameter();
                        paramDepartmentCultureEn.ParameterName = DbProviderHelper.NormalizeParameterName(Provider, "@DepartmentCultureIdEn");
                        paramDepartmentCultureEn.Value = CultureIdEn;
                        paramDepartmentCultureEn.DbType = DbType.Int64;
                        paramDepartmentCultureEn.Direction = ParameterDirection.Input;
                        cmd.Parameters.Add(paramDepartmentCultureEn);

                        var paramDepartmentAr = cmd.CreateParameter();
                        paramDepartmentAr.ParameterName = DbProviderHelper.NormalizeParameterName(Provider, "@DepartmentAr");
                        paramDepartmentAr.Value = departmentAr;
                        paramDepartmentAr.DbType = DbType.String;
                        paramDepartmentAr.Direction = ParameterDirection.Input;
                        cmd.Parameters.Add(paramDepartmentAr);

                        var paramDepartmentCultureIdAr = cmd.CreateParameter();
                        paramDepartmentCultureIdAr.ParameterName = DbProviderHelper.NormalizeParameterName(Provider, "@DepartmentCultureIdAr");
                        paramDepartmentCultureIdAr.Value = CultureIdAr;
                        paramDepartmentCultureIdAr.DbType = DbType.Int64;
                        paramDepartmentCultureIdAr.Direction = ParameterDirection.Input;
                        cmd.Parameters.Add(paramDepartmentCultureIdAr);
                        var paramSiteId = cmd.CreateParameter();
                        paramSiteId.ParameterName = DbProviderHelper.NormalizeParameterName(Provider, "@SiteId");
                        paramSiteId.Value = SiteId;
                        paramSiteId.DbType = DbType.Int64;
                        paramSiteId.Direction = ParameterDirection.Input;
                        cmd.Parameters.Add(paramSiteId);
                        var AdditionalInfo = cmd.CreateParameter();
                        AdditionalInfo.ParameterName = DbProviderHelper.NormalizeParameterName(Provider, "@AdditionalInfo");
                        AdditionalInfo.Value = departmentId;
                        AdditionalInfo.DbType = DbType.String;
                        AdditionalInfo.Direction = ParameterDirection.Input;
                        cmd.Parameters.Add(AdditionalInfo);

                        await connection.OpenAsync();
                        using var reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            DbDataReader result = reader;

                            empId = Convert.ToInt32(result["emp_id_param"].ToString());


                        }
                    }
                }
                return empId;

            }
            catch (Exception ex)
            {
                logger.LogInformation("CreateOrGetCustomerGroupAsync : Error   : " + ex.Message);
                throw ex;
            }
        }
        public async Task<long> CreateOrGetRoleAsync(string department, long RoleType, string connectionString)
        {
            try
            {

                long empId = 0;
                using (var connection =dbFactory.CreateConnection(connectionString,Provider))
                {
                    using (var cmd =dbFactory.CreateCommand("CreateOrGetRole", connection,Provider))
                    {
                        DbProviderHelper.ConfigureCommand(cmd, Provider);
                        cmd.CommandType = CommandType.StoredProcedure;

                        var paramDepartment = cmd.CreateParameter();
                        paramDepartment.ParameterName = DbProviderHelper.NormalizeParameterName(Provider, "@Department");
                        paramDepartment.Value = department;
                        paramDepartment.DbType = DbType.String;
                        paramDepartment.Direction = ParameterDirection.Input;
                        cmd.Parameters.Add(paramDepartment);

                        var paramSiteId = cmd.CreateParameter();
                        paramSiteId.ParameterName = DbProviderHelper.NormalizeParameterName(Provider, "@SiteId");
                        paramSiteId.Value = SiteId;
                        paramSiteId.DbType = DbType.Int64;
                        paramSiteId.Direction = ParameterDirection.Input;
                        cmd.Parameters.Add(paramSiteId);

                        var paramRoleType = cmd.CreateParameter();
                        paramRoleType.ParameterName = DbProviderHelper.NormalizeParameterName(Provider, "@RoleType");
                        paramRoleType.Value = RoleType;
                        paramRoleType.DbType = DbType.Int64;
                        paramRoleType.Direction = ParameterDirection.Input;
                        cmd.Parameters.Add(paramRoleType);
                        await connection.OpenAsync();
                        using var reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            DbDataReader result = reader;

                            empId = Convert.ToInt32(result["emp_id_param"].ToString());


                        }
                    }
                }
                return empId;

            }
            catch (Exception ex)
            {
                logger.LogInformation("CreateOrGetRoleAsync : Error   : " + ex.Message);
                throw ex;
            }
        }
        public async Task AddUserRolesSP(int userId, int userRoleId, long RoleType)
        {
            try
            {


                using (var connection = dbFactory.CreateConnection(DefaultConnection, Provider))
                {
                    using (var command = dbFactory.CreateCommand("AddUserRolesSP", connection, Provider))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add(dbFactory.CreateParameter(Provider, "@InRoleId", userRoleId));
                        command.Parameters.Add(dbFactory.CreateParameter(Provider, "@InUserId", userId));
                        command.Parameters.Add(dbFactory.CreateParameter(Provider, "@RoleType", Convert.ToInt32(RoleType)));
                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation("AddUserRolesSP : Error   : " + ex.Message);

            }
        }
        public async Task AddUserGroupSP(int userId, int userGroupId)
        {
            try
            {


                using (var connection = dbFactory.CreateConnection(DefaultConnection, Provider))
                {
                    using (var command = dbFactory.CreateCommand("AddUserGroupSP", connection, Provider))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add(dbFactory.CreateParameter(Provider, "@InGroupId", userGroupId));
                        command.Parameters.Add(dbFactory.CreateParameter(Provider, "@InUserId", userId));

                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation("AddUserGroupSP : Error   : " + ex.Message);

            }
        }

        public async Task DeleteUserSP(string Username)
        {
            try
            {


                using (var connection = dbFactory.CreateConnection(DefaultConnection, Provider))
                {
                    using (var command = dbFactory.CreateCommand("DeleteUserSP", connection, Provider))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add(dbFactory.CreateParameter(Provider, "@InputNormalizedUserName", Username.ToUpper()));

                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation("DeleteUser : Error   : " + ex.Message);

            }
        }
        public async Task UpdateParentSP(string UserName, int UserId)
        {
            try
            {

                using (var connection = dbFactory.CreateConnection(DefaultConnection, Provider))
                {
                    using (var command = dbFactory.CreateCommand("UpdateParentId", connection, Provider))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        command.Parameters.Add((dbFactory.CreateParameter(Provider, "@username_param", UserName)));
                        command.Parameters.Add((dbFactory.CreateParameter(Provider, "@user_id_param", UserId)));
                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation("UpdateParentSP : Error   : " + ex.Message);


            }
        }
        public async Task<List<EskaCMS.Engine.Models.CultureViewModel>> GetCurrentCulturesSP(int SiteId)
        {
            try
            {
                List < EskaCMS.Engine.Models.CultureViewModel > model = new List<EskaCMS.Engine.Models.CultureViewModel>();


                using (var connection = dbFactory.CreateConnection(DefaultConnection, Provider))
                {
                    using (var command = dbFactory.CreateCommand("GetCurrentCulturesSP", connection, Provider))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add((dbFactory.CreateParameter(Provider, "@SiteId_param", SiteId)));
                        await connection.OpenAsync();
                        using var reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            DbDataReader result = reader;
                            EskaCMS.Engine.Models.CultureViewModel data = new EskaCMS.Engine.Models.CultureViewModel();
                            data.CultureId = Convert.ToInt32(result["CultureId"].ToString());
                            data.CultureCode = result["CultureCode"].ToString();

                            model.Add(data);
                        }
                    }
                }
                return model;
            }
            catch (Exception ex)
            {
                logger.LogInformation("GetCurrentCulturesSP : Error   : " + ex.Message);
                return null;

            }
        }
        private async Task<UserViewModel> fillUserDataFromSP(DbDataReader result, UserViewModel data, bool IsFirstRound)
        {
            try
            {


                if (IsFirstRound)
                {
                    data.Id = Convert.ToInt64(result["Id"].ToString());
                    data.FullName = result["FullName"].ToString();
                    data.ParentId = !string.IsNullOrEmpty(result["ParentId"]?.ToString()) ? Convert.ToInt64(result["ParentId"]?.ToString()) : null;
                    data.UserName = result["UserName"].ToString();
                    data.NormalizedUserName = result["NormalizedUserName"].ToString();
                    data.Email = result["Email"].ToString();
                    data.NormalizedEmail = result["NormalizedEmail"].ToString();
                    data.Status = Convert.ToInt32(result["Status"].ToString());

                }

                var CustomerGroupId = Convert.ToInt64(result["CustomerGroupId"].ToString());
                if (data.CustomerGroupIds.Where(c => c == CustomerGroupId).Count() == 0)
                {
                    data.CustomerGroupIds.Add(CustomerGroupId);
                }

                var RoleId = Convert.ToInt64(result["RoleId"].ToString());
                if (data.RoleIds.Where(c => c == RoleId).Count() == 0)
                {
                    data.RoleIds.Add(RoleId);
                }


                return data;
            }
            catch (Exception ex)
            {
                logger.LogInformation("fillUserDataFromSP : Error   : " + ex.Message);
                return new UserViewModel();
            }
        }
        private async Task<RegisterationResponseDTO> InsertUserDCMS(RegisterViewModel adUser)
        {
            try
            {
                int newUserId;

                using (HttpClient client = _httpClientFactory.CreateClient())
                {
                    string url = configuration.GetValue<string>("DCMSAPIs:BaseURL") + configuration.GetValue<string>("DCMSAPIs:LoginAPI");
                    var contenttest = adUser.SerializeObject();
                    var content = new StringContent(contenttest, Encoding.UTF8, "application/json");

                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiU3lzdGVtIFVzZXIiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJmLnNpcmhhbkBlc2thZGVuaWEuY29tIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbW9iaWxlcGhvbmUiOiIiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJBZG1pbiwiLCJVc2VybmFtZSI6ImYuc2lyaGFuQGVza2FkZW5pYS5jb20iLCJVc2VyVHlwZSI6IkFkbWluIiwiRGVmYXVsdFNoaXBwaW5nQWRkcmVzc0lkIjoiIiwiQ3VycmVuY3lDdWx0dXJlSWQiOiIiLCJDdWx0dXJlSWQiOiIiLCJSb2xlSWRzIjoiMSIsIklzUGFyZW50IjoiMSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2RhdGVvZmJpcnRoIjoiIiwiZXhwIjoxMTIxMzU1MzEwNiwiaXNzIjoibG9jYWxob3N0In0.-9vzn06IQ5S3wcNvsmr6rpWjs09maBPGIhQaRnDJwNI");
                    client.DefaultRequestHeaders.Add("siteId", SiteId.ToString());
                    client.DefaultRequestHeaders.Add("IntegrationPermission", configuration.GetValue<string>("DCMSAPIs:IntegrationPermission"));


                    HttpResponseMessage response = await client.PostAsync(url, content);

                    string responseBody = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        DCMSRegisterResponse<RegisterationResultDTO> Result = JsonConvert.DeserializeObject<DCMSRegisterResponse<RegisterationResultDTO>>(responseBody);
                        return new RegisterationResponseDTO(Convert.ToInt64(EncryptDecryptHelper.Decrypt(Result.content.UserId)), Result.message);
                    }
                    else
                    {
                        DCMSRegisterResponse<string> Result = JsonConvert.DeserializeObject<DCMSRegisterResponse<string>>(responseBody);

                        throw new Exception(Result.message);
                    }
                }

            }
            catch (Exception ex)
            {
                logger.LogInformation("InsertUserDCMS : Error   : " + ex.Message);
                return new RegisterationResponseDTO(0, "");
            }
        }

        public async Task<List<string>> GetAllUsernamesAsync()
        {
            var usernames = new List<string>();

            using (var connection = dbFactory.CreateConnection(DefaultConnection, Provider))
            {
                using (var command = dbFactory.CreateCommand("GetAllUsernamesSP", connection, Provider))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            usernames.Add(reader.GetString("UserName"));
                        }
                    }
                }
            }

            return usernames;
        }

        public async Task AddUpdateUserOptionsSP(long userId, string Name, string Value)
        {
            try
            {


                using (var connection = dbFactory.CreateConnection(DefaultConnection, Provider))
                {
                    using (var command = dbFactory.CreateCommand("AddUpdateUserOptionsSP", connection, Provider))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(((dbFactory.CreateParameter(Provider, "@Name", Name))));
                        command.Parameters.Add(((dbFactory.CreateParameter(Provider, "@UserId", userId))));
                        command.Parameters.Add(((dbFactory.CreateParameter(Provider, "@Value", Value))));
                        command.Parameters.Add((dbFactory.CreateParameter(Provider, "@currentDateTime", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))));

                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation("AddUserRolesSP : Error   : " + ex.Message);

            }
        }
        public async Task CreateUser(ActiveDirectoryViewModel adUser, HREmployee? employee)
        {


            try
            {

                var UserOptions = new List<UserOptionsVM>();
                if (employee != null)
                {
                    if (employee.JOB_TITLE_NAME != null)
                    {
                        var UserOption_JOB_TITLE_NAME = new UserOptionsVM
                        {

                            IsEditable = false,
                            ControlType = 0,
                            PropertyName = "JOB_TITLE_NAME",
                            IsVisible = false,
                            Value = employee.JOB_TITLE_NAME,
                        };
                        UserOptions.Add(UserOption_JOB_TITLE_NAME);

                    }
                    if (employee.EMPLOYEE_RANK != null)
                    {
                        var UserOption_JOB_TITLE_NAME = new UserOptionsVM
                        {

                            IsEditable = false,
                            ControlType = 0,
                            PropertyName = "EMPLOYEE_RANK",
                            IsVisible = false,
                            Value = employee.EMPLOYEE_RANK.ToString(),
                        };
                        UserOptions.Add(UserOption_JOB_TITLE_NAME);

                    }

                    if (employee.PHOTO_PATH != null)
                    {
                        var UserOption_JOB_TITLE_NAME = new UserOptionsVM
                        {

                            IsEditable = false,
                            ControlType = 0,
                            PropertyName = "PHOTO_PATH",
                            IsVisible = false,
                            Value = employee.PHOTO_PATH,
                        };
                        UserOptions.Add(UserOption_JOB_TITLE_NAME);

                    }

                 


                    if (employee.ID != null)
                    {
                        var UserOptionemployee = new UserOptionsVM
                        {

                            IsEditable = false,
                            ControlType = 0,
                            PropertyName = "EmployeeId",
                            IsVisible = false,
                            Value = employee.ID.ToString(),
                        };
                        UserOptions.Add(UserOptionemployee);

                    }


                    if (employee.BIRTHDATE != null)
                    {
                        var UserOptionemployee = new UserOptionsVM
                        {

                            IsEditable = false,
                            ControlType = 0,
                            PropertyName = "BIRTHDATE",
                            IsVisible = false,
                            Value = employee.BIRTHDATE.ToString(),
                        };
                        UserOptions.Add(UserOptionemployee);

                    }

                    if (employee.MANAGER_ID != null)
                    {
                        var UserOptionemployee = new UserOptionsVM
                        {

                            IsEditable = false,
                            ControlType = 0,
                            PropertyName = "MANAGER_ID",
                            IsVisible = false,
                            Value = employee.MANAGER_ID==null?"": employee.MANAGER_ID.ToString(),
                        };
                        UserOptions.Add(UserOptionemployee);

                    }
                    if (employee.NAME2 != null)
                    {
                        var UserOptionemployee = new UserOptionsVM
                        {

                            IsEditable = false,
                            ControlType = 0,
                            PropertyName = "NAME2",
                            IsVisible = false,
                            Value = employee.NAME2,
                        };
                        UserOptions.Add(UserOptionemployee);

                    }
                    if (employee.PHONE2 != null)
                    {
                        var UserOptionemployee = new UserOptionsVM
                        {

                            IsEditable = false,
                            ControlType = 0,
                            PropertyName = "PHONE2",
                            IsVisible = false,
                            Value = employee.PHONE2,
                        };
                        UserOptions.Add(UserOptionemployee);

                    }
                    if (employee.START_DATE != null)
                    {
                        var UserOptionemployee = new UserOptionsVM
                        {

                            IsEditable = false,
                            ControlType = 0,
                            PropertyName = "START_DATE",
                            IsVisible = false,
                            Value = employee.START_DATE.ToString(),
                        };
                        UserOptions.Add(UserOptionemployee);

                    }

                    var UserAvailabilityStatus = new UserOptionsVM
                    {

                        IsEditable = true,
                        ControlType = 0,
                        PropertyName = "AvailabilityStatus",
                        IsVisible = true,
                        Value = ((int)EUserAvailabilityStatus.Available).ToString(),
                    };
                    UserOptions.Add(UserAvailabilityStatus);




                }
                var dept = new
                {

                    DepartmentId = employee.CRG_DPT_ID
                };

                string EnDepName = !string.IsNullOrEmpty(employee.DEPARTMENT_NAME) ? employee.DEPARTMENT_NAME : employee.DEPARTMENT_NAME2;
                string ArDepName = !string.IsNullOrEmpty(employee.DEPARTMENT_NAME2) ? employee.DEPARTMENT_NAME2 : employee.DEPARTMENT_NAME;
                var groupId = await CreateOrGetCustomerGroupAsync(EnDepName, ArDepName, JsonConvert.SerializeObject(dept), DefaultConnection);
                long RoleId = 1;
                if (!string.IsNullOrEmpty(EnDepName))
                {
                    RoleId= await CreateOrGetRoleAsync(EnDepName, RoleType, DefaultConnection);
                }


                var user = new RegisterViewModel
                {
                    UserName = employee.USERNAME,
                    Email = employee.COMPANY_EMAIL,
                         FullName = GetFirstAndLastName(employee.NAME)
                 ,
                    PhoneNumber = adUser.voiceTelephoneNumber,
                    Status = 5,
                    Password = "Eska@" + adUser.sID.value,
                    EnableTwoFactorAuthentication = false,
                    RoleId = RoleId,
                    GroupId = groupId,
                    UserOptions = UserOptions


                };

                RegisterationResponseDTO UserId = await InsertUserDCMS(user);


            }
            catch (Exception e)
            {
                logger.LogInformation("CreateUser : Error   : " + e.Message);

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
        public async Task UpdateUserGeneralInfo(long UserId, string FullName, string PhoneNumber, string Email)
        {
            try
            {
                using (var connection = dbFactory.CreateConnection(DefaultConnection, Provider))
                {
                    using (var command = dbFactory.CreateCommand("UpdateUserGeneralInfoSP", connection, Provider))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(dbFactory.CreateParameter(Provider, "@Email", Email));
                        command.Parameters.Add(dbFactory.CreateParameter(Provider, "@Name", FullName));
                        command.Parameters.Add(dbFactory.CreateParameter(Provider, "@PhoneNumber", PhoneNumber));
                        command.Parameters.Add(dbFactory.CreateParameter(Provider, "@UserId", UserId));
                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation("UpdateUserGeneralInfo : Error   : " + ex.Message);

            }
        }
  

        #endregion
        #region Core Integration
        public async Task<EskaCoreBaseResponseVM<CoreAuthResponseVM>> Authenticate()
        {
            try
            {

                using (var client = _httpClientFactory.CreateClient())
                {
                    client.BaseAddress = new Uri(baseApiUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                    //POST Method
                    CoreAuthViewModel requestVM = new CoreAuthViewModel
                    {
                        BranchId = Convert.ToInt32(BranchId),
                        CompanyId = Convert.ToInt32(CompanyId),
                        Username = CoreUser,
                        Password = CorePassword,
                        IncludeRefreshToken = true
                    };
                    var request = System.Text.Json.JsonSerializer.Serialize(requestVM);
                    var requestContent = new StringContent(request, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("api/CoreAPIAuthenticator/Login", requestContent);
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    EskaCoreBaseResponseVM<CoreAuthResponseVM> returnResponse = System.Text.Json.JsonSerializer.Deserialize<EskaCoreBaseResponseVM<CoreAuthResponseVM>>(responseContent);
                    return returnResponse;

                }
            }
            catch (Exception e)
            {
                logger.LogInformation("Authenticate : Error   : " + e.Message);

                throw e;
            }

        }
        public async Task<EskaCoreBaseResponseVM> InsertUser(UserForm user, string token = "")
        {
            try
            {

                var Auth = await Authenticate();
                using (var client = _httpClientFactory.CreateClient())
                {
                    client.BaseAddress = new Uri(baseApiUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", !string.IsNullOrEmpty(token) ? token : Auth.content.Token);

                    //POST Method  
                    EskaCoreInsertUserVM requestVM = new EskaCoreInsertUserVM
                    {
                        BIRTH_DATE = DateTime.Now,
                        CREATED_BY = "ESKA DCMS",
                        CRG_BRN_ID = Convert.ToInt32(BranchId),
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
                        USERNAME = user.UserName.ToUpper(),
                        P_CHANNEL = 1,
                        P_CREDIT_USER_LIMIT = 0,
                        P_APPLICATION_ID = 32,
                        P_DEBIT_USER_LIMIT = 0,
                        P_USER_TYPE = 1,
                        P_IS_EXTERNAL_USER = 0
                    };
                    var request = System.Text.Json.JsonSerializer.Serialize(requestVM);
                    var requestContent = new StringContent(request, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("api/v1/User/InsertUsers", requestContent);
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    EskaCoreBaseResponseVM returnResponse = System.Text.Json.JsonSerializer.Deserialize<EskaCoreBaseResponseVM>(responseContent);
                    InsertUserBranchVM insertUserBranchVM = new InsertUserBranchVM
                    {
                        CRG_COM_ID = Convert.ToInt32(CompanyId),
                        CRG_BRN_ID = Convert.ToInt32(BranchId),
                        Username = user.UserName.ToUpper(),
                        CREATED_BY = "DCMS Background service"
                    };
                    await InsertUserBranch(insertUserBranchVM);
                    return returnResponse;

                }

            }
            catch (Exception ex)
            {
                logger.LogInformation("InsertUser : Error   : " + ex.Message);

                return new EskaCoreBaseResponseVM();
            }
        }
        public async Task<EskaCoreBaseResponseVM> InsertUserGroup(InsertCoreGroupVM requestVM)
        {
            try
            {

                var Token = await Authenticate();
                using (var client = _httpClientFactory.CreateClient())
                {
                    client.BaseAddress = new Uri(baseApiUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token.content.Token);

                    //POST Method  

                    var request = System.Text.Json.JsonSerializer.Serialize(requestVM);
                    var requestContent = new StringContent(request, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("api/v1/Groups/InsertUserGroup", requestContent);
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    EskaCoreBaseResponseVM returnResponse = System.Text.Json.JsonSerializer.Deserialize<EskaCoreBaseResponseVM>(responseContent);
                    return returnResponse;

                }

            }
            catch (Exception ex)
            {
                logger.LogInformation("InsertUserGroup : Error   : " + ex.Message);

                throw ex;
            }
        }
        public async Task<EskaCoreBaseResponseVM> InsertUserBranch(InsertUserBranchVM requestVM)
        {
            try
            {

                using (var client = _httpClientFactory.CreateClient())
                {
                    client.BaseAddress = new Uri(baseApiUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", requestVM.AuthToken);

                    //POST Method  

                    var request = System.Text.Json.JsonSerializer.Serialize(requestVM);
                    var requestContent = new StringContent(request, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("api/v1/Branch/InsertUserBranches", requestContent);
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    EskaCoreBaseResponseVM returnResponse = System.Text.Json.JsonSerializer.Deserialize<EskaCoreBaseResponseVM>(responseContent);
                    return returnResponse;

                }

            }
            catch (Exception ex)
            {
                logger.LogInformation("InsertUserBranch : Error   : " + ex.Message);

                throw ex;
            }
        }
        public async Task<EskaCoreBaseResponseVM<List<CoreGroupViewModel>>> GetUserInfo(string authToken, string username)
        {
            try
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
                        EskaCoreBaseResponseVM<List<CoreGroupViewModel>> returnResponse = System.Text.Json.JsonSerializer.Deserialize<EskaCoreBaseResponseVM<List<CoreGroupViewModel>>>(responseContent);
                        return returnResponse;
                    }
                    else
                    {
                        var Token = await Authenticate();

                        EskaCoreBaseResponseVM<CoreAuthResponseVM> authResponse = Token;
                        return await GetUserInfo(authResponse.content.Token, username);
                    }
                }

            }
            catch (Exception ex)
            {
                logger.LogInformation("GetUserInfo : Error   : " + ex.Message);

                throw ex;
            }
        }


        #endregion
    }
}
