using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Data;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using Hangfire.MemoryStorage.Database;
using EskaCMS.Infrastructure.Pagination;
using System.Diagnostics;
using EskaCMS.Entities.Entity.Notifications;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Helpers;
using Microsoft.IdentityModel.Tokens;
using System.Web;
using EskaCMS.Entities.Entity.Core;
using Google.Apis.Storage.v1.Data;
using EskaCMS.DataAccess.Data;
using EskaCommerce.Notifications.Repository.NotificationRepository.Interface;
using EskaCommerce.Notifications.Models.NotificationModels;

namespace EskaCommerce.Notifications.Repository.NotificationRepository
{
    /// <summary>
    /// Implements <see cref="INotificationStore"/> using repositories.
    /// </summary>
    public class NotificationRepository : INotificationRepository
    {
        protected DbContext Context { get; }

        private readonly IRepository<NotificationScheme> _notificationSchemeRepository;
        private readonly IRepository<NotificationDetail> _notificationDetailRepository;
        private readonly IRepositoryWithTypedId<UserNotification, Guid> _userNotificationRepository;
        private readonly IRepository<NotificationSubscription> _notificationSubscriptionRepository;
        private readonly IWorkContext _workContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationRepository"/> class.
        /// </summary>
        public NotificationRepository(EskaDCMSDbContext context,
            IRepository<NotificationScheme> notificationRepository,
            IRepository<NotificationDetail> tenantNotificationRepository,
            IRepositoryWithTypedId<UserNotification, Guid> userNotificationRepository,
            IWorkContext workContext,
            IRepository<NotificationSubscription> notificationSubscriptionRepository)
        {
            Context = context;
            _workContext = workContext;
            _notificationSchemeRepository = notificationRepository;
            _notificationDetailRepository = tenantNotificationRepository;
            _userNotificationRepository = userNotificationRepository;
            _notificationSubscriptionRepository = notificationSubscriptionRepository;
        }
        public void SaveChanges()
        {
            Context.SaveChanges();
        }

        public Task SaveChangesAsync()
        {
            return Context.SaveChangesAsync();
        }

        public virtual Task InsertSubscriptionAsync(NotificationSubscription subscription)
        {
            _notificationSubscriptionRepository.Add(subscription);
            return Task.CompletedTask;
        }

        public virtual Task DeleteSubscriptionAsync(long userId, string notificationName, string entityTypeName, string entityId)
        {
            var notificationSubscription = _notificationSubscriptionRepository.Query().FirstOrDefault(s =>
                s.UserId == userId &&
                s.NotificationName == notificationName &&
                s.EntityTypeName == entityTypeName &&
                s.EntityId == entityId);

            if (notificationSubscription != null)
            {
                notificationSubscription.IsDeleted = true;
                //_notificationSubscriptionRepository.Remove(notificationSubscription);
            }

            return Task.CompletedTask;
        }

        public virtual Task InsertNotificationSchemeAsync(NotificationScheme notification)
        {
            _notificationSchemeRepository.Add(notification);
            return Task.CompletedTask;
        }

        public virtual async Task<NotificationScheme> GetNotificationSchemeOrNullAsync(long notificationId)
        {
            return await _notificationSchemeRepository.Query().FirstOrDefaultAsync(s => !s.IsDeleted && s.Id == notificationId);
        }

        public virtual Task InsertUserNotificationAsync(UserNotification userNotification)
        {
            _userNotificationRepository.Add(userNotification);
            return Task.CompletedTask;
        }

        public virtual async Task<List<NotificationSubscription>> GetSubscriptionsAsync(string notificationName, string entityTypeName, string entityId)
        {
            return await _notificationSubscriptionRepository.Query().Where(s =>
                !s.IsDeleted &&
                s.NotificationName == notificationName &&
                s.EntityTypeName == entityTypeName &&
                s.EntityId == entityId
                ).ToListAsync();
        }

        public virtual async Task<List<NotificationSubscription>> GetSubscriptionsAsync(long userId)
        {
            return await _notificationSubscriptionRepository.Query().Where(s => !s.IsDeleted && s.UserId == userId).ToListAsync();
        }

        public virtual async Task<bool> IsSubscribedAsync(long userId, string notificationName, string entityTypeName, string entityId)
        {
            return await _notificationSubscriptionRepository.Query().CountAsync(s =>
                !s.IsDeleted &&
                s.UserId == userId &&
                s.NotificationName == notificationName &&
                s.EntityTypeName == entityTypeName &&
                s.EntityId == entityId
                ) > 0;
        }

        public virtual async Task UpdateUserNotificationStateAsync(Guid userNotificationId, UserNotificationState state)
        {
            var userNotification = await _userNotificationRepository.Query().FirstOrDefaultAsync(s => s.Id == userNotificationId);
            if (userNotification == null)
            {
                return;
            }

            userNotification.State = state;
            await SaveChangesAsync();
        }

        public virtual async Task UpdateAllUserNotificationStatesAsync(long userId, UserNotificationState state)
        {
            var userNotifications = await _userNotificationRepository.Query().Where(s => s.UserId == userId).ToListAsync();

            foreach (var userNotification in userNotifications)
            {
                userNotification.State = state;
            }
            await SaveChangesAsync();
        }

        public virtual async Task DeleteUserNotificationAsync(Guid userNotificationId)
        {
            var userNotification = await _userNotificationRepository.Query().FirstOrDefaultAsync(s => s.Id == userNotificationId);
            if (userNotification == null)
            {
                return;
            }

            userNotification.IsDeleted = true;
            //_userNotificationRepository.Remove(userNotification);
        }

        public virtual Task DeleteAllUserNotificationsAsync(long userId)
        {
            var userNotifications = _userNotificationRepository.Query().Where(s => s.UserId == userId);
            foreach (var userNotification in userNotifications)
            {
                userNotification.IsDeleted = true;
                //_userNotificationRepository.Remove(userNotification);
            }
            return Task.CompletedTask;
        }

        public virtual async Task<List<UserNotificationWithNotificationDetail>> GetUserNotificationWithNotificationDetailsAsync(long userId, UserNotificationState? state = null, int skipCount = 0, int maxResultCount = int.MaxValue)
        {
            var query = from userNotification in _userNotificationRepository.Query()
                        join notificationDetail in _notificationDetailRepository.Query() on userNotification.NotificationDetailId equals notificationDetail.Id
                        where !userNotification.IsDeleted && userNotification.UserId == userId && (state == null || userNotification.State == state.Value)
                        orderby notificationDetail.CreatedOn descending
                        select new { userNotification, notificationDetail };

            query = query.Skip(skipCount).Take(maxResultCount);

            var list = await query.ToListAsync();

            return list.Select(a => new UserNotificationWithNotificationDetail(a.userNotification, a.notificationDetail)).ToList();
        }


        public virtual async Task<int> GetUserNotificationCountAsync(long userId, string notificationType, UserNotificationState? state = null)
        {
            var siteId = _workContext.GetCurrentSiteId();
            return await _userNotificationRepository.Query().CountAsync(s => s.NotificationDetail.NotificationScheme.SiteId == siteId &&!s.IsDeleted &&
                                                                            s.NotificationDetail.Severity != NotificationSeverity.Error&&
                                                                           s.UserId == userId &&
                                                                           (!string.IsNullOrEmpty(notificationType) ? s.NotificationDetail.NotificationName.Contains(notificationType) : true) &&
                                                                           (state == null || s.State == state.Value));
        }

        public virtual async Task<UserNotificationWithNotificationDetail> GetUserNotificationWithNotificationDetailOrNullAsync(Guid userNotificationId)
        {
            var query = from userNotification in _userNotificationRepository.Query()
                        join notificationDetail in _notificationDetailRepository.Query() on userNotification.NotificationDetailId equals notificationDetail.Id
                        where !userNotification.IsDeleted && userNotification.Id == userNotificationId
                        select new { userNotification, notificationDetail };

            var item = await query.FirstOrDefaultAsync();
            return item == null ? null : new UserNotificationWithNotificationDetail(item.userNotification, item.notificationDetail);
        }


        public async Task<bool> UpdateSpecificUserNotificationStates( long userId,List<QuickSearchVM> notifications)
        {

			if (notifications == null || !notifications.Any())
				return false;

			var filteredNotifications = new List<UserNotification>();

			foreach (var item in notifications)
			{
				filteredNotifications.Add(new UserNotification
				{
					Id = item.Id,
					UserId = userId,
					CreatedOn = item.CreatedOn,
					NotificationDetailId = item.NotificationDetailsId,
					IsDeleted = item.IsDeleted,
					State = UserNotificationState.Read
				});
			}

			_userNotificationRepository.UpdateRange(filteredNotifications);

			await _userNotificationRepository.SaveChangesAsync();

			return true;
         }

        public async Task<PaginationResult<QuickSearchVM>> GetSmartSearchNotifications(SmartTableParam param, long SiteId)
        {

            var query = _userNotificationRepository
                                                .Query()
                                                .Include(x => x.NotificationDetail)
                                                .ThenInclude(x => x.NotificationScheme)
                                                .Where(x => SiteId == x.NotificationDetail.NotificationScheme.SiteId)
                                                .Include(x => x.User)
                                                 .Select(
                                                x => new QuickSearchVM
                                                {

                                                    Data = x.NotificationDetail.Data,
                                                    NotificationStatus = x.State,
                                                    CreatedBy = new CreatedByUserVM
                                                    {
                                                        Id = x.NotificationDetail.CreatedBy.Id,
                                                        Name = x.NotificationDetail.CreatedBy.FullName,
                                                        UserName = x.NotificationDetail.CreatedBy.UserName

                                                    },
                                                    Severity = x.NotificationDetail.Severity,
                                                    //   Topics = x.User.CustomerGroups.Select(x => x.CustomerGroup.FireBaseTopic).ToList(),
                                                    NotificationDetailsId = x.NotificationDetailId,
                                                    NotificationSchemeId = x.NotificationDetail.NotificationSchemeId,
                                                    Type = x.NotificationDetail.NotificationName,
                                                    CreatedOn = x.NotificationDetail.CreatedOn,
                                                    PhoneNumber = x.User.PhoneNumber,
                                                    Email = x.User.Email,

                                                })
                                                .AsNoTracking();


            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.Name != null && search.Name != "")
                {
                    string name = search.Name;
                    query = query.Where(x => x.Data.Contains(name));
                }
                if (search.CreatedBy != null && search.CreatedBy != "")
                {
                    string createdBy = search.CreatedBy;
                    query = query.Where(x => x.CreatedBy.Name.Contains(createdBy));
                }

                if (search.UserName != null && search.UserName != "")
                {
                    string UserName = search.UserName;
                    query = query.Where(x => x.CreatedBy.UserName.Contains(UserName));
                }

                //if (search.Group != null && search.Group != "")
                //{
                //    string group = search.Group;
                //    query = query.Where(x => x.User.CustomerGroups.Any(x => x.CustomerGroup.FireBaseTopic == group));

                //}
                if (search.NotificationName != null && search.NotificationName != "")
                {
                    string notificationName = search.NotificationName;
                    query = query.Where(x => x.Type.Contains(notificationName));

                }
                if (search.Severity != null && search.Severity != "")
                {
                    NotificationSeverity severity = search.Severity;
                    query = query.Where(x => x.Severity == severity);

                }
                if (search.Type != null && search.Type != "")
                {
                    string type = search.Type;
                    query = query.Where(x => x.Type.Contains(type));

                }
                //if (search.Topic != null && search.Topic != "")
                //{
                //    string topic = search.Topic;

                //    query = query.Where(x => x.User.CustomerGroups.Any(x => x.CustomerGroup.FireBaseTopic == topic.ToString()));

                //}

                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        query = query.Where(x => x.CreatedOn >= after);
                    }

                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        query = query.Where(x => x.CreatedOn <= before.AddHours(23).AddMinutes(59).AddSeconds(59));
                    }


                }
                if (search.Phone != null)
                {
                    string Phone = search.Phone;
                    query = query.Where(x => x.PhoneNumber.Contains(Phone));
                }

                if (search.Email != null)
                {
                    string Email = search.Email;
                    query = query.Where(x => x.Email.Contains(Email));
                }
            }

            var gridData = await query.OrderByDescending(x => x.NotificationDetailsId).PaginateAsync(param.Pagination.Number, param.Pagination.Start);
            return gridData;
        }
        public async Task<GetNotficationResult> GetNotifications(int PageSize, int PageNumber, long UserId, NotificationType notification)
        {
            try
            {
                var NotificationType = Enum.GetName(typeof(NotificationType), notification);
                var Result = new GetNotficationResult();
                var Notification = _notificationSchemeRepository
                                                                   .Query().AsNoTracking().Include(x => x.CreatedBy).Include(x => x.EmailTemplate)
                                                                   .Where(x => x.NotificationName == NotificationType)
                                                                   .Where(x => Convert.ToInt64(x.UserIds) == UserId);

                Result.Notifications = await Notification
                                                                  .OrderByDescending(x => x.Id)
                                                                  .Select(x => new GetNotifications
                                                                  {
                                                                      content = x.Data,
                                                                      NotificationId = x.Id,
                                                                      UserId = x.CreatedById,
                                                                      FullName = x.CreatedBy.FullName,
                                                                      UnRead = x.IsRead,
                                                                      EncryptedUserId = HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(x.CreatedById.ToString()))
                                                                      ,
                                                                      Color = x.EmailTemplate == null ? null : x.EmailTemplate.Color
                                                                      ,
                                                                      Icon = x.EmailTemplate == null ? null : x.EmailTemplate.Icon,
                                                                      CreationDate = x.CreatedOn,

                                                                  }).Skip(PageNumber * PageSize).Take(PageSize).ToListAsync();
                Result.UnReadNotifcations = await Notification.Where(x => !x.IsRead).CountAsync();
                Result.TotalNotification = await Notification.CountAsync();


                return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        public async Task<GetNotficationResult> GetLastNotifications(int PageSize, int PageNumber, long UserId, NotificationType notification)
        {
            try
            {
                var NotificationType = Enum.GetName(typeof(NotificationType), notification);
                var Result = new GetNotficationResult();
                var Notification = _notificationSchemeRepository
                                                                   .Query()
                                                                   .Include(x => x.EmailTemplate)
                                                                   .Include(x => x.CreatedBy)
                                                                   .AsNoTracking()
                                                                   .Where(x => x.NotificationName == NotificationType)
                                                                   .Where(x => Convert.ToInt64(x.UserIds) == UserId)
                                                                   .OrderByDescending(x => x.Id);
                Result.Notifications = await Notification
                                                                  .OrderByDescending(x => x.Id)
                                                                  .Select(x => new GetNotifications
                                                                  {
                                                                      content = x.Data,
                                                                      NotificationId = x.Id,
                                                                      UserId = x.CreatedById,
                                                                      FullName = x.CreatedBy.FullName,
                                                                      UnRead = x.IsRead,
                                                                      EncryptedUserId = HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(x.CreatedById.ToString())),
                                                                      Color = x.EmailTemplate == null ? null : x.EmailTemplate.Color,
                                                                      Icon = x.EmailTemplate == null ? null : x.EmailTemplate.Icon,
                                                                      CreationDate = x.CreatedOn

                                                                  }).OrderByDescending(x => x.NotificationId).Skip(PageNumber * PageSize).Take(PageSize).ToListAsync();
                Result.UnReadNotifcations = await Notification.Where(x => !x.IsRead).CountAsync();
                Result.TotalNotification = await Notification.CountAsync();


                return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        public async Task<PaginationResult<QuickSearchVM>> GetNotificationsForWeb(SmartTableParam param, long SiteId, long userId)
        {

            var query = _userNotificationRepository
                                              .Query()
                                              .Include(x => x.NotificationDetail)
                                              .ThenInclude(x => x.NotificationScheme)
                                              .Include(x => x.User)
                                              .Where(x => x.UserId == userId)
                                              .Where(x => SiteId == x.NotificationDetail.NotificationScheme.SiteId
                                             && x.NotificationDetail.Severity != NotificationSeverity.Error
                                              && (x.NotificationDetail.NotificationName != "SignalR" || x.NotificationDetail.NotificationName != "WebNotification")).Include(x => x.User)
                                                 .Select(
                                                x => new QuickSearchVM
                                                {
                                                    Id = x.Id,
                                                    Data = x.NotificationDetail.Data,
													IsDeleted = x.IsDeleted,
                                                    NotificationStatus = x.State,
                                                    CreatedBy = new CreatedByUserVM
                                                    {
                                                        Id = x.NotificationDetail.CreatedBy.Id,
                                                        Name = x.NotificationDetail.CreatedBy.FullName,
                                                        UserName = x.NotificationDetail.CreatedBy.UserName

                                                    },
                                                    Severity = x.NotificationDetail.Severity,
                                                    //   Topics = x.User.CustomerGroups.Select(x => x.CustomerGroup.FireBaseTopic).ToList(),
                                                    NotificationDetailsId = x.NotificationDetailId,
                                                    NotificationSchemeId = x.NotificationDetail.NotificationSchemeId,
                                                    Type = x.NotificationDetail.NotificationName,
                                                    CreatedOn = x.NotificationDetail.CreatedOn,
                                                    PhoneNumber = x.User.PhoneNumber,
                                                    Email = x.NotificationDetail.CreatedBy.Email

                                                }).AsNoTracking();
                                              

        


            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.Name != null && search.Name != "")
                {
                    string name = search.Name;
                    query = query.Where(x => x.Data.Contains(name));
                }
                if (search.CreatedBy != null && search.CreatedBy != "")
                {
                    string createdBy = search.CreatedBy;
                    query = query.Where(x => x.CreatedBy.Name.Contains(createdBy));
                }

                if (search.UserName != null && search.UserName != "")
                {
                    string UserName = search.UserName;
                    query = query.Where(x => x.CreatedBy.UserName.Contains(UserName));
                }

                //if (search.Group != null && search.Group != "")
                //{
                //    string group = search.Group;
                //    query = query.Where(x => x.User.CustomerGroups.Any(x => x.CustomerGroup.FireBaseTopic == group));

                //}
                if (search.NotificationName != null && search.NotificationName != "")
                {
                    string notificationName = search.NotificationName;
                    query = query.Where(x => x.Type.Contains(notificationName));

                }
                if (search.Severity != null && search.Severity != "")
                {
                    NotificationSeverity severity = search.Severity;
                    query = query.Where(x => x.Severity == severity);

                }
                if (search.Type != null && search.Type != "")
                {
                    string type = search.Type;
                    query = query.Where(x => x.Type.Contains(type));

                }
                if (search.Status != null)
                {
                    UserNotificationState Status = search.Status;
                    query = query.Where(x => x.NotificationStatus == Status);

                }

                //if (search.Topic != null && search.Topic != "")
                //{
                //    string topic = search.Topic;

                //    query = query.Where(x => x.User.CustomerGroups.Any(x => x.CustomerGroup.FireBaseTopic == topic.ToString()));

                //}

                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        query = query.Where(x => x.CreatedOn >= after);
                    }

                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        query = query.Where(x => x.CreatedOn <= before);
                    }


                }
                if (search.Phone != null) 
                {
                    string Phone = search.Phone;
                    query = query.Where(x => x.PhoneNumber.Contains(Phone));
                }
            }

            var gridData = await query.OrderByDescending(x => x.CreatedOn).PaginateAsync(param.Pagination.Number, param.Pagination.Start);
			var userNotificationTobeUpdated = gridData.Data.ToList();


            if (userNotificationTobeUpdated.Any())
            {
                await UpdateSpecificUserNotificationStates(userId, userNotificationTobeUpdated);
            }


            return gridData;
        }

        public virtual IQueryable<QuickSearchVM> GetNotificationById(long id)
        {
            IQueryable<QuickSearchVM> query = _userNotificationRepository.Query().Include(x => x.User).ThenInclude(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup)
                 .Include(x => x.NotificationDetail).ThenInclude(x => x.NotificationScheme)
                 .Where(x => x.NotificationDetail.Id == id)
              .Select(x => new QuickSearchVM
              {
                  Data = x.NotificationDetail.Data,
                  CreatedBy = new CreatedByUserVM
                  {
                      Id = x.NotificationDetail.CreatedBy.Id,
                      Name = x.NotificationDetail.CreatedBy.FullName
                  },
                  Severity = x.NotificationDetail.Severity,
                  Topics = x.User.CustomerGroups.ToList().Select(x => x.CustomerGroup.FireBaseTopic),
                  NotificationDetailsId = x.NotificationDetailId,
                  NotificationSchemeId = x.NotificationDetail.NotificationSchemeId,
                  Type = x.NotificationDetail.NotificationName
              });

            return query;
        }


        public virtual Task InsertNotificationDetailAsync(NotificationDetail tenantNotificationInfo)
        {
            _notificationDetailRepository.Add(tenantNotificationInfo);
            return Task.CompletedTask;
        }

        public virtual Task DeleteNotificationAsync(NotificationScheme notification)
        {
            notification.IsDeleted = true;
            //_notificationSchemeRepository.Remove(notification);
            return Task.CompletedTask;
        }

        public async Task UpdateNotification(long? notificationSchemeId,long userId,bool MarkAllNotificationsAsRead)
        {
            try
            {
                List<NotificationScheme> Notifictions=new List<NotificationScheme>();
                var condition = MarkAllNotificationsAsRead ? "All" : "Specific";

                switch (condition)
                {
                    case "All":
                        Notifictions = await _notificationSchemeRepository
                            .Query()
                            .Where(x => Convert.ToInt64(x.UserIds) == userId)
                            .ToListAsync();
                        break;

                    case "Specific":
                        Notifictions = await _notificationSchemeRepository
                            .Query()
                            .Where(x => x.Id == notificationSchemeId)
                            .ToListAsync();
                        break;

                    default:
                        break;

                }
                foreach (var notificationScheme in Notifictions)
                {
                    notificationScheme.IsRead = true;
                }
                _notificationSchemeRepository.UpdateRange(Notifictions);
                await _notificationSchemeRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
    }
}
