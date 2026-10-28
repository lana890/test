using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Filters.Helpers.Interfaces;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Security.Services
{
    public class UsersPasswordHistoryService : IUsersPasswordHistoryService
    {
        private readonly IWorkContext _workContext;
        private readonly IRepository<UsersPasswordHistory> _UsersPasswordHistoryRepo;
        private readonly IRepository<UsersSites> _UserSitesRepo;
        private readonly IHashingHelper _HashingHelper;

        public UsersPasswordHistoryService(IWorkContext workContext, IRepository<UsersPasswordHistory> UsersPasswordHistoryRepo, IHashingHelper HashingHelper, IRepository<UsersSites> userSitesRepo)
        {
            _workContext = workContext;
            _UsersPasswordHistoryRepo = UsersPasswordHistoryRepo;
            _HashingHelper = HashingHelper;
            _UserSitesRepo = userSitesRepo;
        }
        public async Task<bool> PasswordHistoryLog(string NewPassword, string OldPassword, ChangePasswordHistory ChangePasswordHistory, long? UserId)
        {
            try
            {

                var SiteId = _workContext.GetCurrentSiteId();


                var UserSiteId = await _UserSitesRepo.Query().Where(x => x.SiteId == SiteId && x.UserId == UserId).Select(c => c.Id).FirstOrDefaultAsync();


                var passwordHistoryChanges = new UsersPasswordHistory()
                {
                    NewPassword = _HashingHelper.GetHash(NewPassword),
                    OldPassword = string.IsNullOrEmpty(OldPassword) ? null : _HashingHelper.GetHash(OldPassword),
                    UsersSitesId = UserSiteId,
                    Date = DateTime.Now,
                    ChangePasswordHistory = ChangePasswordHistory
                };

                _UsersPasswordHistoryRepo.Add(passwordHistoryChanges);
                await _UsersPasswordHistoryRepo.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {

                throw e;
            }

        }







    }
}
