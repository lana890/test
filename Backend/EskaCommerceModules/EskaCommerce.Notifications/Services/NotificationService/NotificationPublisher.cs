using System;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Extensions;
using EskaCommerce.HangfireJobs.Services;
using EskaCommerce.Notifications.Jobs;
using Microsoft.AspNetCore.Identity;
using EskaCMS.Entities.Entity.Notifications;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using EskaCommerce.Notifications.Repository.NotificationRepository.Interface;
using EskaCommerce.Notifications.Models.NotificationModels;

namespace EskaCommerce.Notifications.Services.NotificationService
{
    /// <summary>
    /// Implements <see cref="INotificationPublisher"/>.
    /// </summary>
    public class NotificationPublisher : INotificationPublisher
    {
        public const int MaxUserCountToDirectlyDistributeANotification = 5;

        private readonly INotificationRepository _notificationRepository;
        private readonly IBackgroundJobManager _backgroundJobManager;
        private readonly INotificationDistributer _notificationDistributer;
        private readonly IWorkContext _workContext;
        private readonly IConfiguration _config;


        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationPublisher"/> class.
        /// </summary>
        public NotificationPublisher(
            INotificationRepository notificationRepository,
            IBackgroundJobManager backgroundJobManager,
            INotificationDistributer notificationDistributer,
            IWorkContext workContext,
            IConfiguration config)
        {
            _notificationRepository = notificationRepository;
            _backgroundJobManager = backgroundJobManager;
            _notificationDistributer = notificationDistributer;
            _workContext = workContext;
            _config = config;
        }

        //Create EntityIdentifier includes entityType and entityId.
        /// <inheritdoc />
        public virtual async Task PublishAsync(
    string notificationName,
    NotificationData data = null,
    EntityIdentifier entityIdentifier = null,
    long siteId = 0,
    NotificationSeverity severity = NotificationSeverity.Info,
    long[] userIds = null,
    long[] excludedUserIds = null,
    string emailTemplateId = null,
    long currentUserId = 0)
        {
            if (string.IsNullOrWhiteSpace(notificationName))
                throw new ArgumentException("NotificationName cannot be null or empty", nameof(notificationName));

            int chunkSize = _config.GetValue<int>("chuncks");

            if (chunkSize <= 0)
                chunkSize = 1000; 

            if (currentUserId == 0)
                currentUserId = await _workContext.GetCurrentUserId();

            var finalUserIds = userIds == null || userIds.Length == 0
                ? new[] { currentUserId }
                : userIds;

            if (finalUserIds.All(id => id == 0))
                throw new InvalidOperationException("No valid user found for notification publishing");

            var schemeIds = new List<long>();

            for (int i = 0; i < finalUserIds.Length; i += chunkSize)
            {
                var userChunk = finalUserIds.Skip(i).Take(chunkSize).ToArray();

                var scheme = new NotificationScheme
                {
                    NotificationName = notificationName,
                    EntityTypeName = entityIdentifier?.Type.FullName,
                    EntityTypeAssemblyQualifiedName = entityIdentifier?.Type.AssemblyQualifiedName,
                    EntityId = entityIdentifier?.Id.ToJsonString(),
                    SiteId = siteId == 0 ? 1 : siteId,
                    Severity = severity,
                    IsRead = false,
                    UserIds = string.Join(",", userChunk),
                    ExcludedUserIds = excludedUserIds?.Any() == true ? string.Join(",", excludedUserIds) : "",
                    Data = data?.ToJsonString(),
                    DataTypeName = data?.GetType().AssemblyQualifiedName,
                    CreatedById = currentUserId,
                    EmailTemplateId = emailTemplateId,
                    EmailTemplateSiteId = siteId == 0 ? 1 : siteId
                };
            


            await _notificationRepository.InsertNotificationSchemeAsync(scheme);

                try
                {
                    await _notificationRepository.SaveChangesAsync();
                    schemeIds.Add(scheme.Id);
                }
                catch (Exception ex)
                {
                }
            }

            if (schemeIds.Any())
                await _notificationDistributer.DistributeAsync(schemeIds);
        }

    }
}
