using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using FirebaseAdmin.Messaging;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Models;
using EskaCMS.Core.Extensions;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Core.Services;
using EskaCMS.Core.Model;
using System.Web;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using EskaCommerce.Notifications.Models.NotificationModels;
using EskaCommerce.Notifications.Services.NotificationService.Interface;
//sing Google.Apis.Storage.v1.Data;

namespace EskaCommerce.Notifications.Services.NotificationService
{
    class PushNotification : IPushNotification
    {
        private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;
        private readonly UserManager<User> _userManager;
        private readonly IWorkContext _workContext;
        private readonly IRepository<CustomerGroup> _userGroupUserrepository;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly IdcmsLogService _dcmsLogService;
        private readonly IRepository<SitesHostNames> _sitesHostNamesRepository;

        public PushNotification(
           IRepositoryWithTypedId<SiteSettings, string> appSettingRepository, UserManager<User> userManager,
           IWorkContext workContext, IRepository<CustomerGroup> userGroupUserrepository,
           INotificationPublisher notificationPublisher,
           IRepository<SitesHostNames> sitesHostNamesRepository,
           IdcmsLogService dcmsLogService)
        {
            _appSettingRepository = appSettingRepository;
            _userManager = userManager;
            _workContext = workContext;
            _userGroupUserrepository = userGroupUserrepository;
            _notificationPublisher = notificationPublisher;
            _dcmsLogService = dcmsLogService;
            _sitesHostNamesRepository = sitesHostNamesRepository;
        }
        public async Task<SendNotificationDto> SendPushNotification(NotificationObject data, long siteId)
        {

            var notificationRespone = new SendNotificationDto();

            notificationRespone.UserIds = data.UserId;

            var SiteId = siteId == 0 ? await _workContext.GetCurrentSiteIdAsync() : siteId;
            var imagePath = data.Attachment.Any() ? await PrefixImagePath(data.Attachment, SiteId) : "";
            var defaultAndroidChannel = await GetDefaultAndroidChannel(SiteId);
            var defaultNotificationSoundPath = await GetDefaultSound(SiteId);





            try
            {

                if (!data.PushNotificationOptions.Topics.Any() && data.SendValue.Any())
                {

                    var allMulticastMessage = new List<MulticastMessage>();
                    var batchSize = 500;
                    for (int i = 0; i < data.SendValue.Count; i += batchSize)
                    {
                        var batchTokens = data.SendValue.Skip(i).Take(batchSize).ToList();



                        var multiCastMessages = new MulticastMessage()
                        {
                            Data = data.PushNotificationOptions.Data != null ? data.PushNotificationOptions.Data : null,
                            Webpush = new WebpushConfig()
                            {


                                Notification = new WebpushNotification()
                                {
                                    RequireInteraction = true,
                                }
                            },

                            Notification = new Notification()
                            {
                                Title = data.MessageTitle,
                                Body = data.MessageBody,

                                ImageUrl = !string.IsNullOrEmpty(imagePath) ? Uri.EscapeUriString(imagePath) : null,


                            },
                            Android = new AndroidConfig()
                            {
                                Priority = Priority.High,


                                Notification = new AndroidNotification()
                                {
                                    // EventTimestamp is a non-nullable DateTime in FirebaseAdmin, so it is always
                                    // serialized. Leaving it unset sends event_time = 0001-01-01T00:00:00Z and the
                                    // Android notification shows that as its date.
                                    EventTimestamp = DateTime.UtcNow,
                                    Sound = !string.IsNullOrEmpty(defaultNotificationSoundPath) ? defaultNotificationSoundPath : null,
                                    ChannelId = !string.IsNullOrEmpty(defaultAndroidChannel) ? defaultAndroidChannel : null,
                                },


                            },
                            Apns = new ApnsConfig()
                            {

                                Aps = new Aps()
                                {

                                    Sound = !string.IsNullOrEmpty(defaultNotificationSoundPath) ? defaultNotificationSoundPath : null,
                                    Alert = new ApsAlert()
                                    {

                                    }
                                }
                            },
                            Tokens = batchTokens
                        };

                        allMulticastMessage.Add(multiCastMessages);

                    }


                    var messaging = FirebaseMessaging.DefaultInstance;


                    if (!data.SendValue.Any())
                    {
                        return notificationRespone;
                    }

                    //for (int index = 0; index < allMulticastMessage.Count; index++)
                    //{
                    //    var batch = allMulticastMessage[index];

                    //    var result = await messaging.SendEachForMulticastAsync(batch);

                    //    notificationRespone.FailureCount += result.FailureCount;
                    //    notificationRespone.SuccessCount += result.SuccessCount;

                    //    // Log or handle the index
                    //    Console.WriteLine($"Processed batch index: {index}, Success: {result.SuccessCount}, Failure: {result.FailureCount}");
                    //}

                    foreach (var batch in allMulticastMessage)
                    {

                        var result = await messaging.SendEachForMulticastAsync(batch);


                        notificationRespone.FailureCount += result.FailureCount;
                        notificationRespone.SuccessCount += result.SuccessCount;
                    }




                    return notificationRespone;

                }

                else
                {

                    var listOfUsers = new List<long>();

                    foreach (var topic in data.PushNotificationOptions.Topics)
                    {

                        var userGroup = _userGroupUserrepository.Query()
                                                                .Where(x => !x.IsDeleted && x.FireBaseTopic.ToLower() == topic.ToLower())
                                                                .Include(x => x.Users)
                                                                .ToList();


                        if (userGroup.Any())
                        {
                            var users = userGroup.SelectMany(x => x.Users).Select(x => x.UserId).ToList();
                            listOfUsers = listOfUsers.Concat(users).ToList();

                            var message = new Message()
                            {
                                Topic = topic,
                                Notification = new Notification()
                                {
                                    Title = data.MessageTitle,
                                    Body = data.MessageBody,
                                }
                            };

                            var messaging = FirebaseMessaging.DefaultInstance;


                            var result = await messaging.SendAsync(message);
                            if (string.IsNullOrEmpty(result))
                            {
                                await _dcmsLogService.AddError("Push Notification ", "", "Failed Send Notification for customer groups has this topic " + topic, data.MessageBody, siteId);
                            }
                        }


                    }



                    notificationRespone.UserIds = listOfUsers.ToArray();
                    return notificationRespone;
                }
            }
            catch (Exception exc)
            {

                await _dcmsLogService.AddError("Push Notification ", exc.Message, "Failed Send Push Notification ", data.MessageBody, siteId);

                throw exc;
            }
        }

        #region Helper
        private async Task<string> PrefixImagePath(List<FileViewModel> Attachment, long siteId)
        {

            var publicUrl = Attachment.FirstOrDefault(x => x.contentType.Contains("image/"))?.publicUrl;


            const string ignoreHostName = "localhost";

            var hostName = _sitesHostNamesRepository.Query()
                                                    .Where(x => x.SiteId == siteId && x.IsActive && !x.HostName.Contains(ignoreHostName))
                                                    .FirstOrDefault()?
                                                    .HostName;
            if (!string.IsNullOrEmpty(hostName) && !string.IsNullOrEmpty(publicUrl))
            {

                var path = hostName.EndsWith('/') ? "https://" + hostName + publicUrl : "https://" + hostName + "/" + publicUrl;

                return path;
            }
            else
            {

                return string.Empty;
            }


            return string.Empty;



        }


        private async Task<string> GetDefaultAndroidChannel(long siteId)
        {

            var module = "PushNotification";
            var pushNotificationsettings = _appSettingRepository.Query().AsNoTracking()
                                                .Where(x => x.SiteId == siteId && x.Module == module)
                                                .ToList();


            if (pushNotificationsettings.Any())
            {

                var androidChannel = "AndroidNotificationChannel";
                var notificationChannel = pushNotificationsettings.FirstOrDefault(x => x.Id == androidChannel)?.Value ?? string.Empty;

                return notificationChannel;

            }




            return string.Empty;



        }


        private async Task<string> GetDefaultSound(long siteId)
        {



            var module = "PushNotification";
            var pushNotificationsettings = await _appSettingRepository.Query().AsNoTracking()
                                                .Where(x => x.SiteId == siteId && x.Module == module)
                                                .ToListAsync();


            if (pushNotificationsettings.Any())
            {

                var defaultNotificationSound = "NotificationSoundPath";
                var filePath = pushNotificationsettings.FirstOrDefault(x => x.Id == defaultNotificationSound)?.Value ?? string.Empty;

                return filePath;

            }

            return string.Empty;



        }



        //public async void HandleAdditionalDataAsync(NotificationObject data, long siteId)
        //{

        //    data.PushNotificationOptions.Data ??= new Dictionary<string, string>();


        //    // handle sound file path
        //    var defaultSoundPath = await GetDefaultSound(siteId);


        //    if (!string.IsNullOrEmpty(defaultSoundPath.value))
        //    {

        //        data.PushNotificationOptions.Data[defaultSoundPath.key] = defaultSoundPath.value;

        //    }



        //}
        #endregion


    }
}
