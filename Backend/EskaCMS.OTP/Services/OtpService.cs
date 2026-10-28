using System.Security.Cryptography;
using EskaCMS.OTP.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Notifications;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.OTP.Enums;
using EskaCMS.OTP.Helpers;
using EskaCMS.OTP.Infrastructure;
using EskaCMS.OTP.Models;
using Microsoft.EntityFrameworkCore;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.OTP.Services
{
    public interface IOtpService
    {
        Task SendCode(SendCodeViewModel model);
        Task VerifyCode(VerifyCodeViewModel model);
        Task<bool> ActivateUser(VerifyCodeViewModel model);
        Task<UserOTPVM> GetUserTokenByUserIdAndPurpose(string ProviderValue, string Purpose);
    }

    public class OtpService : IOtpService
    {
        private readonly IRepository<User> _userrepository;
        private readonly IWorkContext _workContext;
        private readonly ITestNotifier _testNotifier;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IRSA_Helper _RSA_Helper;
        private readonly IUserOTPService _UserOTPService;

        public OtpService(
            IRepository<User> userrepository,
            IWorkContext workContext,
            ITestNotifier testNotifier,
            IEmailTemplateService emailTemplateService,
            IRSA_Helper RSA_Helper,
            IUserOTPService UserOTPService)
        {
            _userrepository = userrepository;
            _workContext = workContext;
            _testNotifier = testNotifier;
            _emailTemplateService = emailTemplateService;
            _RSA_Helper = RSA_Helper;
            _UserOTPService = UserOTPService;
        }

        public async Task SendCode(SendCodeViewModel model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.Value) && model.ValidateUser)
                    model.Value = _RSA_Helper.Decrypt(model.Value);

                long SiteId = _workContext.GetCurrentSiteId();
                long userId = await _workContext.GetCurrentUserId();
                if (SiteId == 0)
                    throw new Exception("SiteId cannot be null");

                if (userId == 0)
                {
                    throw new Exception("Something went wrong!");
                }

                string Code = string.Empty;
                var message = "Your security code is: ";
                Code = Get6DigitsOTP();
                await _UserOTPService.AddUserOTPAsync(userId, SiteId, model, Code);

                message = message + Code;

                if (!string.IsNullOrEmpty(model.EmailTemplateId))
                {
                    var emailTemplateObj = await _emailTemplateService.GetEmailTemplateById(model.EmailTemplateId);

                    if (emailTemplateObj != null && emailTemplateObj.EmailBody != null)
                    {
                        if (emailTemplateObj.EmailBody.IndexOf("[codel]") != -1)
                            emailTemplateObj.EmailBody = emailTemplateObj.EmailBody.Replace("[codel]", Code);

                        if (emailTemplateObj.EmailBody.IndexOf("[urll]") != -1 && !string.IsNullOrEmpty(model.Url))
                            emailTemplateObj.EmailBody = emailTemplateObj.EmailBody.Replace("[urll]", model.Url);

                        if (emailTemplateObj.EmailBody.IndexOf("[purposel]") != -1 && !string.IsNullOrEmpty(model.Purpose))
                            emailTemplateObj.EmailBody = emailTemplateObj.EmailBody.Replace("[purposel]", model.Purpose);
                        else
                        {
                            message += " Purpose: " + model.Purpose;
                        }

                        if (emailTemplateObj.EmailBody.IndexOf("[dateTime]") != -1)
                            emailTemplateObj.EmailBody = emailTemplateObj.EmailBody.Replace("[dateTime]", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

                        if (model.AdditionalInfo != null)
                        {
                            foreach (var info in model.AdditionalInfo)
                            {
                                if (emailTemplateObj.EmailBody.IndexOf(info.Key) != -1 && !string.IsNullOrEmpty(info.Value))
                                {
                                    emailTemplateObj.EmailBody = emailTemplateObj.EmailBody.Replace(info.Key, info.Value);
                                }
                            }
                        }
                    }

                    message = emailTemplateObj.EmailBody;
                }

                if (model.SelectedProvider == E2FAProviders.Email)
                {
                    NotificationObject data = new NotificationObject();
                    List<long> Ids = new List<long>();
                    List<string> SendValues = new List<string>();
                    Ids.Add(userId);
                    SendValues.Add(model.Value);
                    data.UserId = Ids.ToArray();
                    data.MessageTitle = "Security Code";
                    data.MessageBody = message;
                    data.SendValue = SendValues;
                    data.Severity = 0;

                    await _testNotifier.SendMessageAsync(NotificationType.Email, null, data, SiteId);
                }
                else if (model.SelectedProvider == E2FAProviders.Phone)
                {
                    if (!string.IsNullOrEmpty(model.Value))
                    {
                        NotificationObject data = new NotificationObject();
                        List<long> Ids = new List<long>();
                        List<string> SendValues = new List<string>();
                        Ids.Add(userId);
                        SendValues.Add(model.Value);
                        data.UserId = Ids.ToArray();
                        data.MessageTitle = "OTP";
                        data.MessageBody = message;
                        data.SendValue = SendValues;
                        data.SMSProvider = model.SMSProvider != null ? model.SMSProvider : SMSProviders.SMSMashastra;
                        data.Severity = 0;
                        await _testNotifier.SendMessageAsync(NotificationType.SMS, null, data, SiteId);
                    }
                }
                else if (model.SelectedProvider == E2FAProviders.WhatsApp)
                {
                    if (!string.IsNullOrEmpty(model.Value))
                    {
                        NotificationObject data = new NotificationObject();
                        List<long> Ids = new List<long>();
                        List<string> SendValues = new List<string>();
                        Ids.Add(userId);
                        SendValues.Add(model.Value);
                        data.UserId = Ids.ToArray();
                        data.MessageTitle = "OTP";
                        data.MessageBody = Code;
                        data.SendValue = SendValues;
                        data.UserName = model.Username;
                        data.WhatsAppProvider = model.WhatsAppProvider != null ? model.WhatsAppProvider : WhatsAppProviders.Infoseed;
                        data.Severity = 0;
                        await _testNotifier.SendMessageAsync(NotificationType.WhatsApp, null, data, SiteId);
                    }
                }
            }
            catch (Exception exc)
            {
                throw new Exception(exc.Message);
            }
        }

        private string Get6DigitsOTP()
        {
            var bytes = new byte[4];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            uint random = BitConverter.ToUInt32(bytes, 0) % 1000000;
            return String.Format("{0:D6}", random);
        }

        public async Task VerifyCode(VerifyCodeViewModel model)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                if (SiteId == 0)
                    throw new Exception("SiteId cannot be null");

                bool result;
                result = await _UserOTPService.VerifyUserTokenAsync(await _workContext.GetCurrentUserId(), SiteId, model.Code, model.Purpose, model.Username);

                if (!string.IsNullOrEmpty(model.Username))
                {
                    var user = await _userrepository.Query().Where(x => x.UserName.ToUpper().Equals(model.Username.ToUpper())).FirstOrDefaultAsync();

                    if (user != null)
                    {
                        user.Status = EStatus.Active;
                        user.StatusDate = DateTimeOffset.Now;
                        _userrepository.Update(user);
                        await _userrepository.SaveChangesAsync();
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

        public async Task<bool> ActivateUser(VerifyCodeViewModel model)
        {
            try
            {
                var user = await _userrepository.Query().Include(x => x.UserOTP)
                      .Select(x => x.UserName == model.Username
                      &&
                      x.UserOTP.Any(o => o.Value == model.Code && o.Purpose == model.Purpose)).FirstOrDefaultAsync();

                if (user)
                {
                    var objuser = new User();
                    objuser.Status = EStatus.Active;
                    objuser.StatusDate = DateTimeOffset.Now;
                    _userrepository.Update(objuser);
                }
                else
                {
                    throw new Exception("Somthing went wrong");
                }
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<UserOTPVM> GetUserTokenByUserIdAndPurpose(string ProviderValue, string Purpose)
        {
            try
            {
                return await _UserOTPService.GetUserOTPByUserIdAndPurpose(ProviderValue, Purpose, _workContext.GetCurrentSiteId());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
