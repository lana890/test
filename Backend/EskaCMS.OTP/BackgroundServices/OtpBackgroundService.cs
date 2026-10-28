using EskaCMS.OTP.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Notifications;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.OTP.Helpers;
using Microsoft.EntityFrameworkCore;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.OTP.BackgroundServices
{
    // Port of EskaCMS.Engine.BaseBackgroundService: polls every 5 seconds, reads its row in
    // Core_BackgroundServiceSettings (Id = service class name) and runs Execute() when the schedule is due.
    public abstract class BaseBackgroundService : BackgroundService
    {
        public readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _BackgroundServiceName;

        public BaseBackgroundService(string BackgroundServiceName, ILogger logger, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _BackgroundServiceName = BackgroundServiceName;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogWarning(_BackgroundServiceName);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

                    BackgroundServiceSettings BGSSetting;
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var backgroundSettingsRepo = scope.ServiceProvider.GetRequiredService<IRepositoryWithTypedId<BackgroundServiceSettings, string>>();
                        BGSSetting = await backgroundSettingsRepo.Query()
                                                                .AsNoTracking()
                                                                .FirstOrDefaultAsync(b => b.Id == _BackgroundServiceName, stoppingToken);
                    }

                    if (BGSSetting != null && BGSSetting.IsActive)
                    {
                        bool DidExecute = await ExecuteOnTime(BGSSetting);
                        if (DidExecute)
                        {
                            using var updateScope = _serviceProvider.CreateScope();
                            var backgroundSettingsRepo = updateScope.ServiceProvider.GetRequiredService<IRepositoryWithTypedId<BackgroundServiceSettings, string>>();
                            var BGSSettingUpdate = await backgroundSettingsRepo.Query()
                                                            .FirstOrDefaultAsync(b => b.Id == _BackgroundServiceName && b.SiteId == BGSSetting.SiteId, stoppingToken);
                            if (BGSSettingUpdate != null)
                            {
                                BGSSettingUpdate.LastExecuted = DateTimeOffset.Now;
                                await backgroundSettingsRepo.SaveChangesAsync();
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception Exc)
                {
                    _logger.LogError(Exc, "Background service {BackgroundServiceName} failed.", _BackgroundServiceName);
                }
            }
        }

        public async Task<bool> ExecuteOnTime(BackgroundServiceSettings Settings)
        {
            bool Execute = false;
            if (Settings.IntervalType == IntervalType.Date)
            {
                if (DateHelper.IsCurrentDay(Settings.IntervalDate))
                {
                    Execute = true;
                }
            }
            else if (Settings.IntervalType == IntervalType.Period)
            {
                switch (Settings.PeriodType)
                {
                    case BGPeriodType.Yearly:
                        if (!Settings.LastExecuted.HasValue ||
                            DateHelper.IsPassedAYear(Settings.LastExecuted.Value))
                        {
                            Execute = true;
                        }
                        break;

                    case BGPeriodType.Monthly:
                        if (!Settings.LastExecuted.HasValue ||
                            DateHelper.IsPassedAMonth(Settings.LastExecuted.Value))
                        {
                            Execute = true;
                        }
                        break;

                    case BGPeriodType.Weekly:
                        if (!Settings.LastExecuted.HasValue ||
                            DateHelper.IsPassedAWeek(Settings.LastExecuted.Value))
                        {
                            Execute = true;
                        }
                        break;

                    case BGPeriodType.Daily:
                        if (!Settings.LastExecuted.HasValue ||
                            DateHelper.IsPassedADay(Settings.LastExecuted.Value))
                        {
                            Execute = true;
                        }
                        break;

                    case BGPeriodType.ByTime:
                        if (!Settings.LastExecuted.HasValue ||
                            DateHelper.IsPassedTimeSpan(Settings.LastExecuted.Value, Settings.TimeSpan))
                        {
                            Execute = true;
                        }
                        break;

                    case BGPeriodType.MonthOfYear:
                        if (Settings.MonthOfYear.HasValue &&
                            DateHelper.IsMonthOfYear(DateTime.Now, Settings.MonthOfYear.Value) &&
                            (!Settings.LastExecuted.HasValue || DateHelper.IsPassedAMonth(Settings.LastExecuted.Value)))
                        {
                            Execute = true;
                        }
                        break;

                    case BGPeriodType.WeekOfMonth:
                        if (Settings.WeekOfMonth.HasValue &&
                            DateHelper.IsWeekOfMonth(DateTime.Now, Settings.WeekOfMonth.Value) &&
                            (!Settings.LastExecuted.HasValue || DateHelper.IsPassedAWeek(Settings.LastExecuted.Value)))
                        {
                            Execute = true;
                        }
                        break;

                    case BGPeriodType.DayOfWeek:
                        if (Settings.DayOfWeek.HasValue &&
                            DateHelper.IsDayOfWeek(DateTime.Now, Settings.DayOfWeek.Value) &&
                            (!Settings.LastExecuted.HasValue || DateHelper.IsPassedADay(Settings.LastExecuted.Value)))
                        {
                            Execute = true;
                        }
                        break;
                }
            }

            if (Execute)
            {
                await this.Execute();
                return true;
            }

            return false;
        }

        public abstract Task<bool> Execute();
    }

    // Port of EskaCMS.Engine.Otp.BGS.OtpBackgroundService: deletes OTP rows older than 60 minutes.
    // Uses the same settings row Id ("OtpBackgroundService"), so it is enabled/scheduled from the same place.
    public class OtpBackgroundService : BaseBackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public OtpBackgroundService(IServiceProvider serviceProvider, ILogger<OtpBackgroundService> logger)
            : base(nameof(OtpBackgroundService), logger, serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override async Task<bool> Execute()
        {
            using var scope = _serviceProvider.CreateScope();
            var userOTPRepository = scope.ServiceProvider.GetRequiredService<IRepository<UserOTP>>();
            var time = DateTimeOffset.Now.AddMinutes(-60);
            var OtpData = userOTPRepository.Query().Where(x => x.CreationDate < time).ToList();
            if (OtpData.Any())
            {
                userOTPRepository.RemoveRange(OtpData);
                await userOTPRepository.SaveChangesAsync();
            }
            return true;
        }
    }
}
