using EskaCMS.OTP.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Notifications;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.OTP.Infrastructure;
using EskaCMS.OTP.Models;
using Microsoft.EntityFrameworkCore;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.OTP.Services
{
    public interface IdcmsLogService
    {
        Task AddInfo(string Module, string Message, string AdditionalInfo, long? SiteId);
        Task AddError(string Module, string exception, string Message, string AdditionalInfo, long? SiteId);
    }

    public class dcmsLogService : IdcmsLogService
    {
        private readonly IRepository<DCMSLog> _dcmsLogRepository;
        private readonly ILogger _logger;
        private readonly IWorkContext _workContext;

        public dcmsLogService(IRepository<DCMSLog> dcmsLogRepository, ILoggerFactory loggerFactory, IWorkContext workContext)
        {
            _dcmsLogRepository = dcmsLogRepository;
            _logger = loggerFactory.CreateLogger<dcmsLogService>();
            _workContext = workContext;
        }

        public async Task AddError(string Module, string exception, string Message, string AdditionalInfo, long? SiteId)
        {
            try
            {
                var Currentplattform = _workContext.GetCurrentPlatform();
                DCMSLog objlog = new DCMSLog();
                objlog.Channel = Currentplattform;
                objlog.AdditionInfo = AdditionalInfo;
                objlog.LogType = LogTypes.Error;
                objlog.SiteId = SiteId;
                objlog.Message = Message;
                objlog.Exception = exception;
                objlog.Module = Module;
                _dcmsLogRepository.Add(objlog);
                await _dcmsLogRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Faild to add error Log/{GetType().Name}: {exc.Message}");
            }
        }

        public async Task AddInfo(string Module, string Message, string AdditionalInfo, long? SiteId)
        {
            try
            {
                var Currentplattform = _workContext.GetCurrentPlatform();
                DCMSLog objlog = new DCMSLog();
                objlog.LogType = LogTypes.Information;
                objlog.AdditionInfo = AdditionalInfo;
                objlog.Channel = Currentplattform;
                objlog.Message = Message;
                objlog.Module = Module;
                objlog.SiteId = SiteId;
                _dcmsLogRepository.Add(objlog);
                await _dcmsLogRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Faild to add error Log/{GetType().Name}: {exc.Message}");
            }
        }
    }

    public interface ISiteSettingsService
    {
        Task<SiteSettingsVM> GetSiteSettingById(string Id, long SiteId);
        Task<EmailsSettings> GetEmailSetting(long SiteId);
    }

    public class SiteSettingsService : ISiteSettingsService
    {
        private readonly IRepositoryWithTypedId<SiteSettings, string> _siteSettingsRepository;
        private readonly IRepository<EmailsSettings> _emailSettings;

        public SiteSettingsService(IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository, IRepository<EmailsSettings> EmailSettings)
        {
            _siteSettingsRepository = siteSettingsRepository;
            _emailSettings = EmailSettings;
        }

        public async Task<SiteSettingsVM> GetSiteSettingById(string Id, long SiteId)
        {
            var setting = await _siteSettingsRepository.Query().Where(x => x.SiteId == SiteId && x.Id.ToLower() == Id.ToLower()).Select(x => new SiteSettingsVM
            {
                Id = x.Id,
                IsVisibleInCommonSettingPage = x.IsVisibleInCommonSettingPage,
                Module = x.Module,
                SiteId = x.SiteId,
                Value = x.Value
            }).FirstOrDefaultAsync();
            return setting;
        }

        public async Task<EmailsSettings> GetEmailSetting(long SiteId)
        {
            try
            {
                var Settings = await _emailSettings.Query().Where(x => (x.SiteId == SiteId || SiteId == 0) && x.Status != EStatus.Deleted && x.IsDefault == true).FirstOrDefaultAsync();

                if (Settings == null)
                {
                    throw new Exception("There are no SMTP Settings for requested Site");
                }
                return Settings;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    public interface IEmailTemplateService
    {
        Task<EmailTemplateVM> GetEmailTemplateById(string Id);
    }

    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IRepositoryWithTypedId<EmailTemplates, string> _emailtemplatesRepository;
        private readonly IWorkContext _workContext;

        public EmailTemplateService(IRepositoryWithTypedId<EmailTemplates, string> emailtemplatesRepository, IWorkContext workContext)
        {
            _emailtemplatesRepository = emailtemplatesRepository;
            _workContext = workContext;
        }

        public async Task<EmailTemplateVM> GetEmailTemplateById(string Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var template = await _emailtemplatesRepository.Query().Where(x => x.Id == Id && (siteId == 0 ? true : x.SiteId == siteId)).Select(x => new EmailTemplateVM
                {
                    Id = x.Id,
                    CultureId = x.CultureId,
                    EmailTemplateTypeId = x.EmailTemplateTypeId,
                    IsHTML = x.IsHTML,
                    SiteId = x.SiteId,
                    Subject = x.Subject,
                    EmailBody = x.EmailBody,
                    AdditionalInfo = x.AdditionalInfo,
                    Color = x.Color,
                    Icon = x.Icon
                }).AsNoTracking().FirstOrDefaultAsync();
                return template;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
    }

    public interface IApiLogService
    {
        Task<bool> CreateLog(ApiLogVM log);
    }

    public class ApiLogService : IApiLogService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ApiLogService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<bool> CreateLog(ApiLogVM log)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var _logRepo = scope.ServiceProvider.GetRequiredService<IRepository<ApiLog>>();

                    ApiLog coreLog = new ApiLog
                    {
                        ApiUrl = log.ApiUrl,
                        Date = DateTimeOffset.Now,
                        Request = log.Request,
                        FullUrl = log.FullUrl,
                        HttpMethod = log.HttpMethod,
                        Response = log.Response,
                        SiteId = log.SiteId,
                        StatusCode = log.StatusCode,
                        UserId = log.UserId
                    };
                    _logRepo.Add(coreLog);
                    await _logRepo.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
