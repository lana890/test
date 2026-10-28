using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EskaCommerce.Notifications.Models;

using EskaCMS.Entities.Entity;
using EskaCommerce.Notifications.Repository.NotificationRepository.Interface;
using EskaCommerce.Notifications.Models.NotificationModels;
using EskaCommerce.Notifications.Services.NotificationService.Interface;
namespace EskaCommerce.Notifications.Services.NotificationService
{
    /// <summary>
    /// Implements  <see cref="IUserNotificationManager"/>.
    /// </summary>
    public class UserNotificationManager : IUserNotificationManager
    {
        private readonly INotificationRepository _notificationRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserNotificationManager"/> class.
        /// </summary>
        public UserNotificationManager(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        /// <inheritdoc />
        public async Task<List<UserNotificationDto>> GetUserNotificationsAsync(long userId, UserNotificationState? state = null, int skipCount = 0, int maxResultCount = int.MaxValue)
        {
            var userNotifications = await _notificationRepository.GetUserNotificationWithNotificationDetailsAsync(userId, state, skipCount, maxResultCount);
            return userNotifications
                .Select(un => un.ToUserNotificationDto())
                .ToList();
        }

        /// <inheritdoc />
        public Task<int> GetUserNotificationCountAsync(long userId, UserNotificationState? state = null)
        {
            return _notificationRepository.GetUserNotificationCountAsync(userId, null,state);
        }

        /// <inheritdoc />
        public async Task<UserNotificationDto> GetUserNotificationAsync(Guid userNotificationId)
        {
            var userNotification = await _notificationRepository.GetUserNotificationWithNotificationDetailOrNullAsync(userNotificationId);

            return userNotification?.ToUserNotificationDto();
        }

        /// <inheritdoc />
        public Task UpdateUserNotificationStateAsync(Guid userNotificationId, UserNotificationState state)
        {
            return _notificationRepository.UpdateUserNotificationStateAsync(userNotificationId, state);
        }

        /// <inheritdoc />
        public Task UpdateAllUserNotificationStatesAsync(long user, UserNotificationState state)
        {
            return _notificationRepository.UpdateAllUserNotificationStatesAsync(user, state);
        }

        /// <inheritdoc />
        public Task DeleteUserNotificationAsync(Guid userNotificationId)
        {
            return _notificationRepository.DeleteUserNotificationAsync(userNotificationId);
        }

        /// <inheritdoc />
        public Task DeleteAllUserNotificationsAsync(long userId)
        {
            return _notificationRepository.DeleteAllUserNotificationsAsync(userId);
        }
    }
}
