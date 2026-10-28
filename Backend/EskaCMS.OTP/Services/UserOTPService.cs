using EskaCMS.OTP.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Notifications;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.OTP.Models;
using Microsoft.EntityFrameworkCore;

namespace EskaCMS.OTP.Services
{
    public interface IUserOTPService
    {
        Task AddUserOTPAsync(long UserId, long SiteId, SendCodeViewModel model, string Code);
        Task<UserOTPVM> GetUserOTPByUserIdAndPurpose(string ProviderValue, string Purpose, long SiteId);
        Task<bool> VerifyUserTokenAsync(long CurrentUser, long SiteId, string Code, string Purpose, string Username, bool isCheckOnResetPassword = false);
    }

    public class UserOTPService : IUserOTPService
    {
        private readonly IRepository<UserOTP> _UserOTPRepository;
        private readonly IRepository<UsersSites> _UsersSitesRepository;
        private readonly IConfiguration _configuration;

        public UserOTPService(IRepository<UserOTP> UserOTPRepository, IRepository<UsersSites> usersSitesRepository, IConfiguration configuration)
        {
            _UserOTPRepository = UserOTPRepository;
            _UsersSitesRepository = usersSitesRepository;
            _configuration = configuration;
        }

        public async Task AddUserOTPAsync(long UserId, long SiteId, SendCodeViewModel model, string Code)
        {
            try
            {
                var CheckUser = await _UsersSitesRepository.Query().AsNoTracking().CountAsync(x => x.UserId == UserId && x.SiteId == SiteId) > 0;
                if (CheckUser)
                {
                    var Validity = _configuration["Authentication:OTPValidity"];
                    Validity = Validity == null ? "2" : Validity;
                    _UserOTPRepository.Add(new UserOTP { UserId = UserId, SiteId = SiteId, Username = model.Username, CreationDate = new DateTimeOffset(DateTimeOffset.Now.DateTime, TimeSpan.Zero), ProviderType = model.SelectedProvider, ProviderValue = model.Value, Purpose = model.Purpose, Validity = TimeSpan.FromMinutes(Convert.ToDouble(Validity)), Value = Code, ExpiryDate = new DateTimeOffset(DateTimeOffset.Now.DateTime.AddMinutes(Convert.ToDouble(Validity)), TimeSpan.Zero) });
                    await _UserOTPRepository.SaveChangesAsync();
                }
                else
                    throw new Exception("User is not assigned to this site");
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<bool> VerifyUserTokenAsync(long CurrentUser, long SiteId, string Code, string Purpose, string Username, bool isCheckOnResetPassword = false)
        {
            try
            {
                var dt = new DateTimeOffset(DateTimeOffset.Now.DateTime, TimeSpan.Zero);
                var code = await _UserOTPRepository.Query()
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

        public async Task<UserOTPVM> GetUserOTPByUserIdAndPurpose(string Username, string Purpose, long SiteId)
        {
            try
            {
                return await _UserOTPRepository.Query()
                    .Include(x => x.User)
                    .ThenInclude(x => x.UserSites)
                         .Where(x => (string.IsNullOrEmpty(Purpose) || x.Purpose.Equals(Purpose))
                         && x.Username.ToLower() == Username.ToLower()
                         && x.User.UserSites.Any(us => us.SiteId == SiteId))
                         .Select(y => new UserOTPVM
                         {
                             UserId = y.User.Id,
                             Username = y.Username,
                             lstOtps = y.User.UserOTP.Select(t => new UserOTPDetailsVM
                             {
                                 CreationDate = t.CreationDate,
                                 ProviderType = t.ProviderType,
                                 ProviderValue = t.ProviderValue,
                                 Value = t.Value,
                                 Purpose = t.Purpose,
                                 Validity = t.Validity,
                                 ExpiryDate = t.ExpiryDate
                             }).Where(x => (string.IsNullOrEmpty(Purpose) || x.Purpose.Equals(Purpose))
                         && y.Username.ToLower() == Username.ToLower()
                         && y.User.UserSites.Any(us => us.SiteId == SiteId)).ToList()
                         }
                         ).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
