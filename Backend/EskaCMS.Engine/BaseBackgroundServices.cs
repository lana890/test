
using EskaCMS.Engine.BGS;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;


namespace EskaCMS.Engine
{
    public abstract class BaseBackgroundService :  Microsoft.Extensions.Hosting.BackgroundService
    {
        public readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _BackgroundServiceName;
        private readonly IConfiguration configuration;

        public BaseBackgroundService(string BackgroundServiceName, ILogger logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _BackgroundServiceName = BackgroundServiceName;
            _logger = logger;
            this.configuration = configuration;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
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


        public async Task<bool> ExecuteOnTime(BackgroundServiceSettings? Settings)
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
            if ("ADUsersSyncBackgroundService"==Settings.Name)
            { }

            if (Execute)
            {
                await this.Execute();
                return true;
            }


            return false;
        }





        //public async Task UpdateCol(string ColName, string value)
        //{

        //    try
        //    {
        //        string connectionString = configuration.GetConnectionString("DefaultConnection");
        //        string queryString = @"Update ""Core_BackgroundServiceSettings"" set """ + ColName + @""" = " + value + @" where ""Id"" = '" + _BackgroundServiceName + @"'";

        //        using (MySqlConnection connection = new MySqlConnection(connectionString))
        //        {
        //            MySqlCommand command = new MySqlCommand(queryString, connection);
        //            command.CommandType = System.Data.CommandType.Text;
        //            command. = System.Data.CommandType.Text;




        //            //try
        //            //{

        //            //    connection.Open();

        //            //    await command.ExecuteNonQueryAsync();


        //            //    connection.Close();
        //            //    command.Dispose();
        //            //}
        //            //catch (Exception e)
        //            //{

        //            //    //connection.Close();
        //            //    //command.Dispose();
        //            //    //throw e;
        //            //}
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        throw ex;
        //    }


        //}




        public abstract Task<bool> Execute();


    }
}
