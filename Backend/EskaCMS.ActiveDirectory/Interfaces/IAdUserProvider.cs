
using EskaDCMSActiveDirectory.Models;
using EskaDCMSActiveDirectory.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

namespace EskaDCMSActiveDirectory.Interfaces
{
    public interface IAdUserProvider
    {
        ActiveDirectoryUser CurrentUser { get; set; }
        bool Initialized { get; set; }
        Task Create(HttpContext context, IConfiguration config);
        Task<ActiveDirectoryUser> GetAdUser(IIdentity identity);
        Task<ActiveDirectoryUser> GetAdUser(string samAccountName);
        Task<ActiveDirectoryUser> GetAdUser(Guid guid);
        Task<List<ActiveDirectoryUser>> GetDomainUsers();
        Task<List<ActiveDirectoryUser>> FindDomainUser(string search);
        Task<CoreLoginResponse> CoreRefreshToken(RefreshCoreTokenVM model);
        Task<LoginWithADVM> Login(ActiveDirectoryUser CurrentUser, long siteId);
        Task<bool> ValidateAd(loginRequestViewModel model);
       // Task<LoginResponse> ADLogin(ActiveDirectoryUser  user);
    }
}

