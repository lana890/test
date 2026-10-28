using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Manage;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Pages.Models;
using EskaCMS.Security.Models;
using EskaCMS.Sites.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Security;

namespace EskaCMS.Security.Services.Interfaces
{
    public interface IUserLoginTransactionsService
    {

        Task<PaginationResult<UserLoginTransactions>> GetUserLogins(PaginationUserLoginTransaction model);
        Task<bool> AddLogUserLogin(long UserId, UserLoggingType loggingType,long? SiteId = null);

    }
}
