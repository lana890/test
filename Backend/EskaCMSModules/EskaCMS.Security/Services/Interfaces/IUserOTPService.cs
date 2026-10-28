using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Entities.Entity;

using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Services.Interfaces
{
    public interface IUserOTPService
    {
        Task AddUserOTPAsync(long UserId, long SiteId, SendCodeViewModel model, string Code);
       // Task<bool> IsOTPExist(long SiteId, string Code, string Purpose, string ProviderValue);
        Task<UserOTPVM> GetUserOTPByUserIdAndPurpose(string ProviderValue, string Purpose, long SiteId);
        Task<bool> VerifyUserTokenAsync(long CurrentUser,long SiteId, string Code, string Purpose, string Username, bool isCheckOnResetPassword = false);
    }


}
