using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Core.Events;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Services
{
    public class TwoFactorAuthenticationServices : ITwoFactorAuthenticationServices
    {
        private readonly IRepository<User> userRepository;
        private readonly IWorkContext workContext;
        private readonly IRepository<UserOTP> userOtpRepository;
        private readonly IUserOTPService userOTPService;
        private readonly IUserLoginTransactionsService userLoginTransactionsService;
        private readonly ITokenService tokenService;
        private readonly IMediator mediator;

        public TwoFactorAuthenticationServices(IRepository<User> userRepository,
            IWorkContext workContext,
            IRepository<UserOTP> UserOtpRepository,
            IUserOTPService userOTPService,
            IUserLoginTransactionsService userLoginTransactionsService,
            ITokenService tokenService,
            IMediator mediator)
        {
            this.userRepository = userRepository;
            this.workContext = workContext;
            userOtpRepository = UserOtpRepository;
            this.userOTPService = userOTPService;
            this.userLoginTransactionsService = userLoginTransactionsService;
            this.tokenService = tokenService;
            this.mediator = mediator;
        }

        public async Task<LoginResponse> VerifyTwoFactorAuthentication(VerifyTwoFactorAuthenticationViewModel model)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();
                if (SiteId == 0)
                    throw new Exception("SiteId cannot be null");

                var User = await userRepository
                                            .Query()
                                            .Where(x => x.NormalizedUserName.Trim() == model.UserName.ToUpper().Trim())
                                            .Include(x=>x.UserSites)
                                            .Include(x=>x.UserRoles)
                                            .ThenInclude(x=>x.Role)
                                            .Include(x => x.CustomerGroups)
                                            .FirstOrDefaultAsync();


                if (User != null)
                {


                    await VerifyCode(new VerifyCodeViewModel { Code = model.Code, Purpose = model.purpose, Username = model.UserName }, SiteId);

                    LoginResponse runTimeObject = new LoginResponse();
                    await mediator.Publish(new UserSignedIn { UserId = User.Id, SiteId = SiteId });
                    await userLoginTransactionsService.AddLogUserLogin(User.Id, UserLoggingType.LogginSuccess);
                    runTimeObject.Culture = GlobalConfiguration.DefaultCulture;
                    runTimeObject.UserAdditionData = User.UserOptions.Select(x => new UserOptionsVM
                    {
                        PropertyName = x.PropertyName,
                        Value = x.Value,
                        ControlType = x.ControlType,
                        IsEditable = x.IsEditable,
                        IsVisible = x.IsVisible,
                        Id = x.Id
                    }).ToList();
                    runTimeObject.Email = User.Email;
                    runTimeObject.Role = User.UserRoles.Select(x => new UserRoleVM { RoleId = x.Role.Id, RoleName = x.Role.Name }).ToList();
                    runTimeObject.Token = await tokenService.CreateToken(User, true);
                    return runTimeObject;

                }
                else
                { 
                
                throw new Exception("Something Went Wrong!!!");
                
                }


                

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task VerifyCode(VerifyCodeViewModel model,long SiteId)
        {

            try
            {
              

                bool result;
                result = await VerifyUserTokenAsync(await workContext.GetCurrentUserId(), SiteId, model.Code, model.Purpose, model.Username);

                if (!string.IsNullOrEmpty(model.Username))
                {
                    var user = await userRepository.Query().Where(x => x.NormalizedUserName.Equals(model.Username.ToUpper())).FirstOrDefaultAsync();

                    if (user != null)
                    {
                        user.Status = EStatus.Active;
                        user.StatusDate = DateTimeOffset.Now;
                        userRepository.Update(user);
                        await userRepository.SaveChangesAsync();
                    }
                }

                if (!result)
                    throw new Exception("Invalid code.");
            }
            catch (Exception exc)
            {

                throw new Exception(exc.Message);
            }
        }

        public async Task<bool> VerifyUserTokenAsync(long CurrentUser, long SiteId, string Code, string Purpose, string Username, bool isCheckOnResetPassword = false)
        {
            try
            {

                var dt = new DateTimeOffset(DateTimeOffset.Now.DateTime, TimeSpan.Zero);
                var code = await userOtpRepository.Query().AsNoTracking()
                                               .Where(otp => otp.Username.ToUpper() == Username.ToUpper()
                                               && otp.SiteId == SiteId
                                               && otp.Purpose.Trim().Equals(Purpose.Trim())
                                               && otp.UserId == CurrentUser
                                               && (otp.ExpiryDate.CompareTo(dt) > 0 || isCheckOnResetPassword)
                                               )
                                               .OrderByDescending(x => x.CreationDate).FirstOrDefaultAsync();
                if (code != null && code.Value == Code)
                    return true;
                else
                    return false;

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
    }
}
