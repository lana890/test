using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCMS.NotificationManager.Services.Interfaces;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Engine.NewsLetter.BGS
{
    public class NewsLetterBackgroundService : BaseBackgroundService
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        private readonly PeriodicTimer _periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(20));
        private readonly CancellationTokenSource tokenSource = new();
        private readonly IConfiguration _configuration;
        public NewsLetterBackgroundService(IServiceProvider serviceProvider
            , ILogger<NewsLetterBackgroundService> logger
            , IConfiguration configuration)
            : base(nameof(NewsLetterBackgroundService), logger, serviceProvider, configuration)
        {
            _serviceProvider = serviceProvider;

            _logger = logger;
            _configuration = configuration;

        }

        public override async Task<bool> Execute()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                INewsLetterService newsLetterService = scope.ServiceProvider.GetRequiredService<INewsLetterService>();



                List<NewsLetters> Newsletters = await newsLetterService.GetNewsLetters();

                foreach (var Newsletter in Newsletters)
                {
                    if (ShouldExecute(Newsletter))
                    {
                        try
                        {
                            NotificationObject Notificationobj = await newsLetterService.CreateNotificationObject(Newsletter);


                            await newsLetterService.SendNewsLetterAsync(Newsletter, Notificationobj);

                            _logger.LogInformation($"Successfully Send NewsLetter:  {Newsletter.Name}");


                            await UpdateSendStatusAfterSend(Newsletter.Id, Entities.Enum.ESendStatus.SentSuccessfully);
                        }
                        catch (Exception)
                        {
                            _logger.LogInformation($"Failed Send NewsLetter:  {Newsletter.Name}");
                            await UpdateSendStatusAfterSend(Newsletter.Id, Entities.Enum.ESendStatus.Faild);
                        }


                    }

                }
            }
            catch (Exception ex)
            {
                using var scope = _serviceProvider.CreateScope();
                var dcmsLogService = scope.ServiceProvider.GetRequiredService<IdcmsLogService>();
                await dcmsLogService.AddError("NewsLetter BackgroundService", "", ex.Message, "", null);

            }
           return true;
        }
        #region Helpers
        private bool ShouldExecute(NewsLetters NewsLetter)
        {
            try
            {
                bool Result = false;

                    var CurrentDate = DateTimeOffset.Now;
                    var CompareSentDate = CurrentDate.CompareTo(NewsLetter.SentDate.Value);
                    bool LastDateIsNull = NewsLetter.LastSentDate == null;

                    switch (NewsLetter.sendTimer)
                    {
                        case Entities.Enum.ESendTimer.OneTime: Result = CompareSentDate > 0 && LastDateIsNull ? true : false; break;
                        case Entities.Enum.ESendTimer.Daily: Result = CompareSentDate > 0 && (LastDateIsNull || CurrentDate.CompareTo(NewsLetter.LastSentDate.Value.AddDays(1)) > 0) ? true : false; break;
                        case Entities.Enum.ESendTimer.Weekly: Result = CompareSentDate > 0 && (LastDateIsNull || CurrentDate.CompareTo(NewsLetter.LastSentDate.Value.AddDays(7)) > 0) ? true : false; break;
                        case Entities.Enum.ESendTimer.Monthly: Result = CompareSentDate > 0 && (LastDateIsNull || CurrentDate.CompareTo(NewsLetter.LastSentDate.Value.AddMonths(1)) > 0) ? true : false; break;
                        case Entities.Enum.ESendTimer.Yearly: Result = CompareSentDate > 0 && (LastDateIsNull || CurrentDate.CompareTo(NewsLetter.LastSentDate.Value.AddYears(1)) > 0) ? true : false; break;
                        default: Result = false; break;
                    }

                
                return Result;

            }
            catch (TaskCanceledException ex)
            {
                _logger.LogInformation(ex.ToString());
                return false;

            }

        }
        private async Task<bool> UpdateSendStatusAfterSend(long NewsLetterId, Entities.Enum.ESendStatus Newstatus)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    var NewsLetterRepo = scope.ServiceProvider.GetRequiredService<IRepository<NewsLetters>>();

                    var query = await NewsLetterRepo.Query().Where(x => x.Id == NewsLetterId).FirstOrDefaultAsync();


                    query.SendStatus = Newstatus;

                    if (Newstatus != Entities.Enum.ESendStatus.Faild)
                    {
                        query.LastSentDate = DateTime.Now;
                    }

                    if (query.sendTimer == Entities.Enum.ESendTimer.OneTime && query.Type == Entities.Enum.NewsLetterType.Scheduled && Newstatus == Entities.Enum.ESendStatus.SentSuccessfully)
                    {
                        query.Type = Entities.Enum.NewsLetterType.Sent;
                    }

                    if (query.sendTimer == Entities.Enum.ESendTimer.OneTime && query.Type == Entities.Enum.NewsLetterType.Rescheduled && Newstatus == Entities.Enum.ESendStatus.SentSuccessfully)
                    {
                        query.Type = Entities.Enum.NewsLetterType.Resent;
                    }
                    NewsLetterRepo.Update(query);
                    NewsLetterRepo.SaveChanges();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex.ToString());
                    return false;
                }
            }
        }
        #endregion


    }

}

