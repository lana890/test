using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Events;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Manage;
using EskaCMS.Core.Services;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Security.Services
{
    public class UserLoginTransactionsService : IUserLoginTransactionsService
    {
        private readonly IWorkContext _workContext;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<UserLoginTransactions> _userLoginTransactions;

        public UserLoginTransactionsService(IWorkContext workContext, IServiceScopeFactory serviceScopeFactory, IRepository<User> userRepository, IRepository<UserLoginTransactions> userLoginTransactions)
        {
            _workContext = workContext;
            _serviceScopeFactory = serviceScopeFactory;
            _userRepository = userRepository;
            _userLoginTransactions = userLoginTransactions;
        }


        public async Task<bool> ExcecuteLogUserLogin(string HostName,UserLoginTransactions data)
        {

            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = _workContext.GetCurrentSiteId();
                var IPAdress = _workContext.GetIPAddress();
                await LogUserLogin(HostName, IPAdress, UserId, SiteId);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        public async Task<PaginationResult<UserLoginTransactions>> GetUserLogins(PaginationUserLoginTransaction model)
        {

            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                return await _userLoginTransactions.Query()
                    .Include(x => x.UserSite)
                    .Where(x => x.UserSite.UserId == model.UserId && x.UserSite.SiteId == SiteId)
                    .OrderByDescending(x => x.LoginDate)
                    .PaginateAsync(model.PageSize, model.PageNumber);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

      

        private async Task<bool> LogUserLogin( string HostName , string UserIP , long UserId, long SiteId)
        {
            try
            {


                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var _UserVisitRepo = scope.ServiceProvider.GetRequiredService<IRepository<UserLoginTransactions>>();
                    var _UserSiteRepo = scope.ServiceProvider.GetRequiredService<IRepository<UsersSites>>();
                    //var user = _userRepository.Query().Where(x => x.Id == UserId).FirstOrDefault();
                  
                    
                    var UserSiteObj =  await  _UserSiteRepo.Query()
                                .Where(u => u.UserId == UserId && u.SiteId == SiteId)
                                .Include(u=>u.Site)
                                .FirstOrDefaultAsync();
                 
                    //_UserNavigationRepo.Add(UserNavigation);
                    //await _UserNavigationRepo.SaveChangesAsync();


                    return true;
                }
            }

            catch (Exception e)
            {
                return false;
            }

        }
        public async Task<bool> AddLogUserLogin(long UserId, UserLoggingType loggingType, long? SiteId = null)
        {
            try
            {       
                    var UserSiteId = await _workContext.GetCurrentUserSiteId(UserId, SiteId);
                    var UserIP = _workContext.GetIPAddress();
              
                    var UserLoginTransaction = new UserLoginTransactions()
                    {
                        UserSiteId = UserSiteId,
                        HostName = _workContext.GetCurrentHostname(), 
                        LoginDate = DateTimeOffset.Now,
                        UserIP = UserIP,
                        LoggingType = loggingType,

                    };
                    _userLoginTransactions.Add(UserLoginTransaction);
                    await _userLoginTransactions.SaveChangesAsync();

                    return true;
             
            }

            catch (Exception e)
            {
                return false;
            }

        }
    }
}
