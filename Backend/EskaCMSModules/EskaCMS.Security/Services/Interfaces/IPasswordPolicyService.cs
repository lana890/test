using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Manage;
using EskaCMS.Pages.Models;
using EskaCMS.Security.Models;
using EskaCMS.Sites.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Services.Interfaces
{
    public interface IPasswordPolicyService
    {
        Task<bool> UpdatePasswordPolicyStatusAsync(long Id, EStatus NewStatus);
        Task<long> UpsertPasswordPolicyAsync(PolicyPasswordVM Model);
        Task<bool> IsValidPasswordAsync(string Password, string UserName);
        Task<PolicyPasswordVM> GetBySiteId(long SiteId);
        Task<ExpandoObject> GetPasswordViolationsAsync(string Password, string UserName);
        Task<List<PolicyPasswordVM>> GetPasswordPolicies();

        Task<GetRetrictedResponseVm> GetRestrictedPasswords(SearchParamsRestrictedVm search);
        Task<long> UpsertRestrictedPasswords(RestrictedPasswordsVM Model);
        Task<bool> UpdateRestrictedPasswordStatus(long Id, EStatus NewStatus);


    }
}
