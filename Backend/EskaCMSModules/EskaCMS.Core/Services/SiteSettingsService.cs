using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Model;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Core.Services
{
    public class SiteSettingsService : ISiteSettingsService
    {
        private readonly IRepositoryWithTypedId<SiteSettings, string> _siteSettingsRepository;
        private readonly IRepository<EmailsSettings> _emailSettings;
        private readonly IWorkContext _WorkContext;

        public SiteSettingsService(IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository, IRepository<EmailsSettings> EmailSettings, IWorkContext workContext)
        {
            _siteSettingsRepository = siteSettingsRepository;
            _emailSettings = EmailSettings;
            _WorkContext = workContext;
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
        public async Task<List<SiteSettingsVM>> GetSiteSettingsByModule(string Module, long SiteId)
        {
            var setting = await _siteSettingsRepository.Query().Where(x => x.SiteId == SiteId && x.Module == Module).Select(x => new SiteSettingsVM
            {
                Id = x.Id,
                IsVisibleInCommonSettingPage = x.IsVisibleInCommonSettingPage,
                Module = x.Module,
                SiteId = x.SiteId,
                Value = x.Value

            }).ToListAsync();
            return setting;
        }
        public async Task<List<SiteSettingsVM>> GetAllSiteSettings(long SiteId)
        {
            var setting = await _siteSettingsRepository.Query().Where(x => x.SiteId == SiteId).Select(x => new SiteSettingsVM
            {
                Id = x.Id,
                IsVisibleInCommonSettingPage = x.IsVisibleInCommonSettingPage,
                Module = x.Module,
                SiteId = x.SiteId,
                Value = x.Value

            }).ToListAsync();
            return setting;
        }

        public async Task<EmailsSettings> GetEmailSetting(long SiteId)
        {
            try
            {
                var Settings = await _emailSettings.Query().Where(x => (x.SiteId == SiteId || SiteId == 0)&&  x.Status!=EStatus.Deleted && x.IsDefault == true).FirstOrDefaultAsync();
                   
                if(Settings == null)
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

        public async Task<string> GetReleaseVersion()
        {
            var Platform = _WorkContext.GetCurrentPlatform();
            var SiteId = await _WorkContext.GetCurrentSiteIdAsync();
            SiteSettingsVM Release;

            if (Platform == EPlatformSource.Mobile)
            {
               Release =  await GetSiteSettingById("CurrentMobileReleaseVersion", SiteId);
            }
            else
            {
                Release = await GetSiteSettingById("CurrentWebReleaseVersion", SiteId);

            }

            return Release.Value;
        }

        //public async Task<EmailConfig> GetEmailSmtpSettings(long SiteId, string SenderModule)
        //{
        //    if (string.IsNullOrEmpty(SenderModule))
        //        SenderModule = "EmailSenderSmpt";
        //    var Settings = await _siteSettingsRepository.Query().Where(x => x.SiteId == SiteId && x.Module == SenderModule).ToListAsync();

        //    if (Settings.Count() > 0)
        //    {

        //        EmailConfig objconfig = new EmailConfig();

        //        objconfig.RequiresAuthentication = Convert.ToBoolean(Settings.Find(x => x.Id == "SmtpRequiresAuthentication").Value);
        //        objconfig.SmtpPassword = Settings.Find(x => x.Id == "SmtpPassword").Value;
        //        objconfig.SmtpPort = Convert.ToInt32(Settings.Find(x => x.Id == "SmtpPort").Value);
        //        objconfig.SmtpServer = Settings.Find(x => x.Id == "SmtpServer").Value;
        //        objconfig.SmtpUsername = Settings.Find(x => x.Id == "SmtpUsername").Value;
        //        objconfig.SenderEmail = Settings.Find(x => x.Id == "SmtpSenderEmail").Value;
        //        return objconfig;

        //    }
        //    else
        //    {
        //        throw new Exception("Error in Site Email Settings");
        //    }

        //}


        //public async Task<EmailConfig> GetAdminEmailSmtpSettings()
        //{

        //    var Settings = await _siteSettingsRepository.Query().Where(x => x.SiteId == 1 &&x.Module == "EmailSenderSmpt").ToListAsync();

        //    if (Settings.Count() > 0)
        //    {

        //        EmailConfig objconfig = new EmailConfig();

        //        objconfig.RequiresAuthentication = Convert.ToBoolean(Settings.Find(x => x.Id == "SmtpRequiresAuthenticationAdmin").Value);
        //        objconfig.SmtpPassword = Settings.Find(x => x.Id == "SmtpPasswordAdmin").Value;
        //        objconfig.SmtpPort = Convert.ToInt32(Settings.Find(x => x.Id == "SmtpPortAdmin").Value);
        //        objconfig.SmtpServer = Settings.Find(x => x.Id == "SmtpServerAdmin").Value;
        //        objconfig.SmtpUsername = Settings.Find(x => x.Id == "SmtpUsernameAdmin").Value;
        //        objconfig.SenderEmail = Settings.Find(x => x.Id == "SmtpSenderEmailAdmin").Value;
        //        return objconfig;

        //    }
        //    else
        //    {
        //        throw new Exception("Error in Site Email Setting ");
        //    }

        //}

    }
}
