using EskaCMS.Core.Events;
using EskaCMS.Core.Extentions.Settings;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.NotificationManager.Services;
using EskaCMS.NotificationManager.Services.Interfaces;
using EskaCMS.SignalR.RealTime;
using EskaCommerce.HangfireJobs.Internal;
using EskaCommerce.HangfireJobs.Services;
using EskaCommerce.Module.Core.Services;
using EskaCommerce.Module.Notifications.Notifiers;
using EskaCommerce.Notifications.Events;
using EskaCommerce.Notifications.Hubs;
using EskaCommerce.Notifications.Jobs;
using EskaCommerce.Notifications.Notifiers;
using EskaCommerce.Notifications.Repository.NotificationRepository;
using EskaCommerce.Notifications.Repository.NotificationRepository.Interface;
using EskaCommerce.Notifications.Services;
using EskaCommerce.Notifications.Services.NotificationService;
using EskaCommerce.Notifications.Services.NotificationService.Interface;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace EskaCommerce.Notifications
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<INotificationRepository, NotificationRepository>();

            services.AddSingleton<INotificationDefinitionManager, NotificationDefinitionManager>();
            services.AddSingleton<IOnlineClientManager, OnlineClientManager>();
            services.AddTransient<IRealTimeNotifier, SignalRRealTimeNotifier>();
            services.AddSingleton<NotificationTracker>();

            services.AddTransient<IBackgroundJobManager, BackgroundJobManager>();
            services.AddTransient<INotificationDistributer, NotificationDistributer>();
            services.AddTransient<ISettingService, SettingService>();

            services.AddTransient<INotificationPublisher, NotificationPublisher>();
            services.AddTransient<INotificationSubscriptionManager, NotificationSubscriptionManager>();
            services.AddTransient<IUserNotificationManager, UserNotificationManager>();

            services.AddTransient<ITestNotifier, TestNotifier>();
            services.AddTransient<INotificationHandler<UserSignedIn>, UserSignedInHandler>();
            services.AddTransient<NotificationDistributionJob>();
            services.AddSingleton<BackgroundJobCollection>();
            services.AddTransient<IPushNotification, PushNotification>();
            services.AddTransient<INewsLetterService, NewsLetterService>();


        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.AddSettingDefinitionItems(SettingDefinitions.DefaultItems());
            app.AddNotificationDefinitionItems(NotificationDefinitions.DefaultItems());
        }
    }
}
