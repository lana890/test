using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Core.Services
{
    public class BackgroundSettingsService : IBackgroundSettingsService
    {
        private readonly DateTimeOffset TimeNow = DateTimeOffset.Now;

        private readonly IRepositoryWithTypedId<BackgroundServiceSettings, string> _backgroundRepository;
        private readonly IWorkContext _workContext;
        public readonly UserManager<User> _userManager;

        public BackgroundSettingsService(
            IRepositoryWithTypedId<BackgroundServiceSettings, string> backgroundRepository,
            IWorkContext workContext,
            UserManager<User> userManager
            )
        {
            _backgroundRepository = backgroundRepository;
            _workContext = workContext;
            _userManager = userManager;
        }
        public async Task<string> Create(BackgroundServiceSettingsCreate request)
        {

            BackgroundServiceSettings bgSettings = new BackgroundServiceSettings
            {
                Id = request.Name.Replace(" ", ""),
                CreatedById = await _workContext.GetCurrentUserId(),
                CreationDate = TimeNow,
                Name = request.Name,
                SiteId = await _workContext.GetCurrentSiteIdAsync(),
                AdditionalData = request.AdditionalData,
                Body = request.Body,
                IntervalDate = request.IntervalDate,
                NotificationType = CoreNotificationType.Email,
                Title = request.Title,
                DayOfWeek = request.DayOfWeek,
                IntervalType = request.IntervalType,
                MonthOfYear = request.MonthOfYear,
                PeriodType = request.PeriodType,
                TimeSpan = (TimeSpan)request.TimeSpan,
                WeekOfMonth = request.WeekOfMonth
            };
            _backgroundRepository.Add(bgSettings);
            await _backgroundRepository.SaveChangesAsync();
            return bgSettings.Id;
        }
        public async Task<string> Update(BackgroundServiceSettingsUpdate request)
        {
            BackgroundServiceSettings bgSettings = _backgroundRepository.Query().Where(x => x.Id == request.Id).FirstOrDefault();

            if (request.NotificationType.HasValue)
            {
                bgSettings.NotificationType = (CoreNotificationType)request.NotificationType;
            }
            if (request.IntervalDate.HasValue)
            {
                bgSettings.IntervalDate = (DateTimeOffset)request.IntervalDate;
            }
            if (request.IntervalType.HasValue)
            {
                bgSettings.IntervalType = (IntervalType)request.IntervalType;
            }
            if (request.PeriodType.HasValue)
            {
                bgSettings.PeriodType = (BGPeriodType)request.PeriodType;
            }
            if (request.MonthOfYear.HasValue)
            {
                bgSettings.MonthOfYear = request.MonthOfYear;
            }
            if (request.WeekOfMonth.HasValue)
            {
                bgSettings.WeekOfMonth = request.WeekOfMonth;
            }
            if (request.DayOfWeek.HasValue)
            {
                bgSettings.DayOfWeek = request.DayOfWeek;
            }
            if (request.TimeSpan.HasValue)
            {
                bgSettings.TimeSpan = (TimeSpan)request.TimeSpan;
            }
            if (!string.IsNullOrEmpty(request.Name))
            {
                bgSettings.Name = request.Name;
            }
            if (!string.IsNullOrEmpty(request.Title))
            {
                bgSettings.Title = request.Title;
            }
            if (!string.IsNullOrEmpty(request.Body))
            {
                bgSettings.Body = request.Body;
            }
            bgSettings.AdditionalData = request.AdditionalData;
            bgSettings.ModificationDate = TimeNow;
            bgSettings.ModifiedById = await _workContext.GetCurrentUserId();
            _backgroundRepository.Update(bgSettings);
            await _backgroundRepository.SaveChangesAsync();
            return bgSettings.Id;
        }
        public async Task<SmartGridOutputVM<BackgroundServiceSettingsList>> GetList(ListRequestVM<BackgroundServiceSettingsListRequest> param)
        {
            BackgroundServiceSettingsListRequest search = param.SearchParams;

            List<BackgroundServiceSettings> docQuery = await _backgroundRepository.Query().AsNoTracking()
                                      .Where(bgSettings =>
                                          (string.IsNullOrEmpty(search.Name) || bgSettings.Name.ToUpper().Contains(search.Name.ToUpper())) &&
                                          (string.IsNullOrEmpty(search.Title) || bgSettings.Title.ToUpper().Contains(search.Title.ToUpper())) &&
                                          ((search.NotificationType == null || search.NotificationType == 0) || bgSettings.NotificationType == search.NotificationType))
                                      .OrderByDescending(d => d.CreationDate)
                                      .ToListAsync();
            SmartGridOutputVM<BackgroundServiceSettingsList> bgSettings = new SmartGridOutputVM<BackgroundServiceSettingsList>
            {
                Items = docQuery.Select(x => new BackgroundServiceSettingsList
                {
                    Id = x.Id,
                    DayOfWeek = x.DayOfWeek,
                    IntervalDate = x.IntervalDate,
                    IntervalType = x.IntervalType,
                    MonthOfYear = x.MonthOfYear,
                    Name = x.Name,
                    NotificationType = x.NotificationType,
                    PeriodType = x.PeriodType,
                    TimeSpan = x.TimeSpan,
                    Title = x.Title,
                    WeekOfMonth = x.WeekOfMonth
                }).Skip(param.PageSize * param.PageIndex).Take(param.PageSize).ToList(),
                TotalRecord = docQuery.Count()
            };

            return bgSettings;

        }
        public async Task<List<BackgroundServiceSettingsList>> GetAll()
        {

            List<BackgroundServiceSettingsList> bgSettings = await _backgroundRepository.Query()
                                      .OrderByDescending(d => d.CreationDate)
                                      .Select(x => new BackgroundServiceSettingsList
                                      {
                                          Id = x.Id,
                                          DayOfWeek = x.DayOfWeek,
                                          IntervalDate = x.IntervalDate,
                                          IntervalType = x.IntervalType,
                                          MonthOfYear = x.MonthOfYear,
                                          Name = x.Name,
                                          NotificationType = x.NotificationType,
                                          PeriodType = x.PeriodType,
                                          TimeSpan = x.TimeSpan,
                                          Title = x.Title,
                                          WeekOfMonth = x.WeekOfMonth
                                      })
                                      .ToListAsync();


            return bgSettings;

        }
        public async Task<BackgroundServiceSettingsResponse> Get(string id)
        {
            BackgroundServiceSettingsResponse bgSettings = await _backgroundRepository.Query()
                .Include(x => x.CreatedBy)
                .Where(bg => bg.Id == id)
                .OrderByDescending(x => x.CreationDate)
                .Select(x => new BackgroundServiceSettingsResponse
                {
                    Id = x.Id,
                    DayOfWeek = x.DayOfWeek,
                    IntervalDate = x.IntervalDate,
                    IntervalType = x.IntervalType,
                    MonthOfYear = x.MonthOfYear,
                    Name = x.Name,
                    NotificationType = x.NotificationType,
                    PeriodType = x.PeriodType,
                    TimeSpan = x.TimeSpan,
                    Title = x.Title,
                    WeekOfMonth = x.WeekOfMonth,
                    AdditionalData = x.AdditionalData,
                    Body = x.Body,
                    CreatedBy = x.CreatedBy.FullName,
                    CreatedById = x.CreatedById,
                    IsActive = x.IsActive,
                    SiteId= x.SiteId
                    
                    
                })
                .FirstOrDefaultAsync();

            return bgSettings;

        }
    }
}