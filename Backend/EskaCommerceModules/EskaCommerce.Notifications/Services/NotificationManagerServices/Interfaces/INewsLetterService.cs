using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.NotificationManager.Models;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.NotificationManager.Services.Interfaces
{
    public interface INewsLetterService
    {
        Task DeleteAsync(long id);
        Task<List<UsersNewsLetter>> GetNewsLetterUsersAsync(long id);
        Task<long?> UpsertNewsLetterAsync(RequestNewsLetterVm newsLetter);
        Task<List<UsersGroupDto>> GetUsersForNewsLetterAsync();
        Task<NewsLetterRecordesDto> GetNewsLetterListAsync(NewsLetterSearchParamsDto Search);
        Task SendNewsLetterAsync(NewsLetters newsLetters, NotificationObject notificationObject);
        Task<NewsLetterResponseEditVm> GetNewsLetterEdit(long newsLetterId);
        Task<NotificationObject> CreateNotificationObject(NewsLetters newsLetter);
        Task<List<NewsLetters>> GetNewsLetters();
        Task<bool> AddSubscriber(long PageId, string ComponentSettingsId, EditUserDto User);

        Task<PaginationResult<NewsLetterDto>> GetSubscribedNotifications(SmartTableParam model);
        Task<bool> MuteSubcribedNotifications(long Id);
        Task<bool> UnMuteSubcribedNotifications(long Id);
        Task<bool> UpdateNewsletterViewerCount(long Id);

        Task<PaginationResult<NewsLetterStatisticsVm>> GetNewsLetterStatistics(long NewsLetterId, SmartTableParam model);

        Task<PaginationResult<NewsLetterViewersVm>> GetNotificationViewers(long statisticId, SmartTableParam model);
        Task<string> GenerateNoificationLink(NotificationLinkVM notificationLinkModel);
    }
}
