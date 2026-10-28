
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.NotificationManager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.NotificationManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

using EskaCMS.Core.Models;
using System.Transactions;
using EskaCMS.Core.Data;
using EskaCommerce.Notifications.Notifiers;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Core.Model;
using Microsoft.AspNetCore.Identity;
using System.Data.SqlClient;

using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Entities.Entity.NewsLetter;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.DataAccess.Data;
using EskaCommerce.Notifications.Models.NotificationModels;
using EskaCMS.Pages.Services.Interfaces;

namespace EskaCMS.NotificationManager.Services
{
    public class NewsLetterService : INewsLetterService
    {
        private readonly IWorkContext _workContext;
        private readonly IRepository<NewsLetters> _newsLettersRepo;
        private readonly IRepository<NewsLetterSubscribers> _newsLetterSubscribersRepo;
        private readonly IRepository<NewsLetterMedias> _newsLetterMediasRepo;
        private readonly IRepository<CustomerGroup> _customerGroup;
        private readonly EskaDCMSDbContext _context;
        private readonly ITestNotifier _testModifier;
        private readonly IRepository<SitesHostNames> sitesHostnameRepository;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IRepository<User> _userrepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPages _Pages;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IRepository<EmailsSettings> _EmailsSettingsRepo;
        private readonly IRepository<NewsLetterGroupSubscribers> _newsLetterGroupSubscribers;
        private readonly IRepository<NewsLetterStatistics> _newsLetterStatisticsRepo;
        private readonly IRepository<NewsLetterViwers> _newsLetterViwerRepo;
        private readonly IRepositoryWithCustomId<CustomerGroupUser> _customerGroupUser;
        private readonly IEmailSender _emailSender;

        public NewsLetterService(
            IRepository<NewsLetters> newsLettersRepo,
            IRepository<NewsLetterSubscribers> newsLetterSubscribersRepo,
            IRepository<NewsLetterMedias> newsLetterMediasRepo,
            IRepository<CustomerGroup> customerGroup,
            ITestNotifier testNotifier,
            IRepository<SitesHostNames> sitesHostnameRepository,
            IRepository<EmailsSettings> EmailsSettingsRepo,
            IEmailTemplateService emailTemplateService,
            EskaDCMSDbContext context,
            IRepository<User> userrepository,
             IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository,
            IServiceProvider serviceProvider,

            IWorkContext workContext,
            IPages Pages,
            IServiceScopeFactory serviceScopeFactory,
             IRepository<NewsLetterGroupSubscribers> newsLetterGroupSubscribers,
            IRepositoryWithCustomId<CustomerGroupUser> customerGroupUser,
            IRepository<NewsLetterStatistics> newsLetterStatisticsRepo,
            IRepository<NewsLetterViwers> newsLetterViwerRepo,
            IEmailSender emailSender
            )
        {
            _newsLettersRepo = newsLettersRepo;
            _newsLetterSubscribersRepo = newsLetterSubscribersRepo;
            _newsLetterMediasRepo = newsLetterMediasRepo;
            _customerGroup = customerGroup;
            _testModifier = testNotifier;
            this.sitesHostnameRepository = sitesHostnameRepository;
            _context = context;
            _EmailsSettingsRepo = EmailsSettingsRepo;
            _workContext = workContext;
            _emailTemplateService = emailTemplateService;
            _userrepository = userrepository;
            this.siteSettingsRepository = siteSettingsRepository;
            _serviceProvider = serviceProvider;
            _Pages = Pages;
            _serviceScopeFactory = serviceScopeFactory;

            _newsLetterGroupSubscribers = newsLetterGroupSubscribers;
            _customerGroupUser = customerGroupUser;
            _newsLetterViwerRepo = newsLetterViwerRepo;
            _newsLetterStatisticsRepo = newsLetterStatisticsRepo;
            _emailSender = emailSender;
        }
        public async Task<long?> UpsertNewsLetterAsync(RequestNewsLetterVm model)
        {
            try
            {
                long userId = await _workContext.GetCurrentUserId();
                long siteId = _workContext.GetCurrentSiteId();
                bool isEditMode = model.Id != 0;

                var newsLetter = await GetOrCreateNewsLetterAsync(model, siteId, isEditMode, userId);

                await UpdateNewsLetterProperties(newsLetter, model, siteId);

                if (!isEditMode)
                {
                    _newsLettersRepo.Add(newsLetter);
                }

                await _newsLettersRepo.SaveChangesAsync();

                await InsertUpdateNewsLetterSubscribersAsync(
                    newsLetter.Id,
                    model.NewsLetterSubscribers?.ToList()
                );

                await InsertUpdateNewsLetterMediasAsync(
                    newsLetter.Id,
                    model.UploadIds?.ToList() ?? new List<long>()
                );

                if (model.NewsLetterGroupSubscribers is not null)
                {
                    await UpsertNewsLetterGroupSubscribesAsync(
                        newsLetter.Id,
                        model.NewsLetterGroupSubscribers.ToList()
                    );
                }

                if (ShouldSendMessage(newsLetter, model))
                {
                    await _context.Entry(newsLetter)
                        .Collection(nl => nl.NewsLetterSubscribers)
                        .Query()
                        .Include(ns => ns.User)
                        .LoadAsync();

                    await _context.Entry(newsLetter)
                        .Collection(nl => nl.NewsLetterMedias)
                        .Query()
                        .Include(ns => ns.Media)
                        .LoadAsync();

                    await SendMessage(newsLetter);
                }

                return newsLetter.Id;
            }
            catch
            {
                throw;
            }
        }

        public async Task<NewsLetterResponseEditVm> GetNewsLetterEdit(long newsLetterId)
        {
            try
            {

                var newsLetterResponse = await _newsLettersRepo
                                                    .Query()
                                                    .Include(x => x.NewsLetterSubscribers)
                                                    .Where(x => newsLetterId == x.Id)
                                                    .Select(x => new NewsLetterResponseEditVm
                                                    {
                                                        Id = x.Id,
                                                        EmailTemplateId = x.EmailTemplateId,
                                                        SentDate = x.SentDate,
                                                        EmailSettingsResponse = new EmailSettingsDto
                                                        {
                                                            Id = x.EmailsSettingsId.HasValue ? x.EmailsSettingsId.Value : null,
                                                            Email = x.EmailsSettingsId.HasValue ? x.EmailsSettings.SmtpSenderEmail : null
                                                        },
                                                        SendTimer = x.sendTimer,
                                                        Status = x.Status,
                                                        NewsLetterBody = x.NewsLetterBody,
                                                        Description = x.Description,
                                                        Name = x.Name,
                                                        NotificationType = x.NotificationType,
                                                        Subject = x.Subject,
                                                        Type = x.Type,
                                                        EndDate = x.EndDate,
                                                        SubscribedGroup = x.GroupSubscribers.Any() ? x.GroupSubscribers.Where(x => !x.CustomerGroup.IsDeleted).
                                                                                                                        Select(c => new SubscribedGroupDto { Id = c.CustomerGroupId, Name = c.CustomerGroup.customerGroupDetails.Select(x => x.Name).FirstOrDefault() })
                                                                                                                        .ToList() : null,
                                                        NotificationLink = x.NotificationLink,
                                                        IsExternalLink = x.IsExternalLink

                                                    }).FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(newsLetterResponse.NotificationLink))
                {
                    newsLetterResponse.NotificationLinkVM = await GetNotificationObj(newsLetterResponse.NotificationLink, newsLetterResponse.IsExternalLink);
                }
                newsLetterResponse.Users = await GetUsers(newsLetterId);

                newsLetterResponse.AttachFile = await GetMedias(newsLetterId);

                return newsLetterResponse;


            }
            catch (Exception ex)
            {
                throw ex;
            }


        }

        private async Task<NotificationLinkVM?> GetNotificationObj(string notificationLink, bool isExternalLink)
        {
            try
            {
                var notificationObj = new NotificationLinkVM();

                if (isExternalLink)
                {
                    notificationObj.ExternalLink = notificationLink;
                    return notificationObj;
                }
                else
                {
                    var SiteHostName = await sitesHostnameRepository.Query().AsNoTracking().Where(x => notificationLink.Contains(x.HostName + "/" + x.Prefix)).FirstOrDefaultAsync();

                    notificationObj.CultureId = SiteHostName.CultureId;

                    var SplitLink = notificationLink.Split('/');
                    notificationObj.NotificationLink = SplitLink[SplitLink.Length - 2] + "/" + SplitLink[SplitLink.Length - 1];

                    return notificationObj;


                }


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<PaginationResult<NewsLetterStatisticsVm>> GetNewsLetterStatistics(long NewsLetterId, SmartTableParam model)
        {


            var newsletterStatistics = _newsLetterStatisticsRepo.Query()
                                                                .Where(x => x.NewsLetterId == NewsLetterId)
                                                                .Include(x => x.NewsLetter)
                                                                .Include(x => x.NewsLetterViwers)
                                                                .AsNoTracking();

            if (model.Search.PredicateObject != null)
            {
                dynamic search = model.Search.PredicateObject;





                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        newsletterStatistics = newsletterStatistics.Where(x => x.CreationDate >= after);
                    }

                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        newsletterStatistics = newsletterStatistics.Where(x => x.CreationDate <= before);
                    }


                }

            }



            var data = await newsletterStatistics.OrderByDescending(x => x.CreationDate)
                                                       .Select(x => new NewsLetterStatisticsVm
                                                       {
                                                           StatisticId = x.Id,
                                                           NotificationName = x.NewsLetter.Name,
                                                           CreationDate = x.CreationDate,
                                                           ExpectedRecipientCount = x.ExpectedRecipientCount,
                                                           FailedNotificationCount = x.FailedNotificationCount,
                                                           ModificationDate = x.ModificationDate,
                                                           ViewerCount = x.NewsLetterViwers.Count(),
                                                           MutedNotificationCount = x.MutedUserCount,
                                                           SuccessNotificationCount = x.SuccessCount,
                                                       })
                                                       .PaginateAsync(model.Pagination.Number, (model.Pagination.Start));

            return data;


        }

        public async Task<PaginationResult<NewsLetterViewersVm>> GetNotificationViewers(long statisticId, SmartTableParam model)
        {


            var newsletterStatistics = _newsLetterViwerRepo.Query()
                                                                .Where(x => x.NewsLetterStatisticId == statisticId)
                                                                .Include(x => x.User)
                                                                .AsNoTracking();

            if (model.Search.PredicateObject != null)
            {
                dynamic search = model.Search.PredicateObject;





                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        newsletterStatistics = newsletterStatistics.Where(x => x.CreationDate >= after);
                    }

                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        newsletterStatistics = newsletterStatistics.Where(x => x.CreationDate <= before);
                    }


                }

                if (search.UserName != null)
                {

                    string UserName = search.UserName;
                    newsletterStatistics = newsletterStatistics.Where(x => x.User.NormalizedUserName.Contains(UserName.ToUpper()));
                }

                if (search.Email != null)
                {

                    string Email = search.Email;
                    newsletterStatistics = newsletterStatistics.Where(x => x.User.NormalizedEmail.Contains(Email.ToUpper()));
                }
                if (search.PhoneNumber != null)
                {

                    string PhoneNumber = search.PhoneNumber;
                    newsletterStatistics = newsletterStatistics.Where(x => x.User.PhoneNumber.Contains(PhoneNumber));
                }

            }



            var data = await newsletterStatistics.OrderByDescending(x => x.CreationDate)
                                                 .Select(x => new NewsLetterViewersVm
                                                 {
                                                     Id = x.UserId,
                                                     Email = x.User.Email,
                                                     PhoneNumber = x.User.PhoneNumber,
                                                     UserName = x.User.UserName,
                                                     ViewDate = x.CreationDate
                                                 })
                                                 .PaginateAsync(model.Pagination.Number, (model.Pagination.Start));

            return data;


        }
        public async Task<List<UsersNewsLetter>> GetNewsLetterUsersAsync(long NewsLetterId)
        {
            try
            {

                var Users = await _newsLetterSubscribersRepo
                                                         .Query()
                                                         .Where(x => x.NewsLetterId == NewsLetterId)
                                                         .Include(x => x.User)
                                                         .Where(x => x.User.Status != EStatus.Deleted)
                                                         .Select(y => new UsersNewsLetter
                                                         {
                                                             Id = y.User.Id,
                                                             UserName = y.User.UserName,
                                                             Email = y.User.Email
                                                         })
                                                         .AsNoTracking()
                                                         .ToListAsync();





                return Users;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<List<UsersGroupDto>> GetUsersForNewsLetterAsync()
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var Users = await _customerGroup
                    .Query()
                    .Include(x => x.customerGroupDetails)
                    .Include(x => x.Users)
                    .ThenInclude(x => x.User)
                    .Where(x => x.SiteId == SiteId && x.Users.Any(x => x.User.Status != EStatus.Deleted))
                    .Select(x => new UsersGroupDto
                    {
                        GroupId = x.Id,
                        GroupName = x.customerGroupDetails.Select(x => x.Name).FirstOrDefault(),
                        Users = x.Users.Select(y => new UsersNewsLetter
                        {
                            Id = y.User.Id,
                            UserName = y.User.UserName,
                            Email = y.User.Email
                        }).ToList()

                    })
                    .AsNoTracking()
                    .ToListAsync();

                return Users;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<NewsLetterRecordesDto> GetNewsLetterListAsync(NewsLetterSearchParamsDto search)
        {
            var result = new NewsLetterRecordesDto();

            var searchText = search.SearchText?.Trim().ToLower();

            var query = _newsLettersRepo
                .Query()
                .AsNoTracking()
             //   .Include(x => x.NewsLetterSubscribers)
                 //   .ThenInclude(x => x.User)
               // .Include(x => x.GroupSubscribers)
                  //  .ThenInclude(x => x.CustomerGroup)
                  //      .ThenInclude(x => x.Users)
                .Where(x => x.Status != EStatus.Deleted);

            // ✅ Apply filters BEFORE execution
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(x => x.Name.ToLower().Contains(searchText));
            }

            if (search.NotificationType.HasValue)
            {
                query = query.Where(x => x.NotificationType == search.NotificationType);
            }

            // ✅ Count BEFORE pagination
            result.TotalRecords = await query.CountAsync();

            // ✅ Pagination + projection BEFORE ToListAsync
            result.NewsLetters = await query
                .OrderByDescending(x => x.CreationDate)
                .Skip(search.PageNumber * search.PageSize)
                .Take(search.PageSize)
                .Select(x => new NewsLetterResultVm
                {
                    Id = x.Id,
                    Name = x.Name,
                    NotificationType = x.NotificationType,

                    //SendTo =
                    //    x.NewsLetterSubscribers
                    //        .Where(s => s.User.Status != EStatus.Deleted)
                    //        .Select(s => s.User.Id)
                    //        .Count()
                    //    +
                    //    x.GroupSubscribers
                    //        .SelectMany(gs => gs.CustomerGroup.Users)
                    //        .Select(u => u.UserId)
                    //        .Distinct()
                    //        .Count(),

                    SendDate = x.SentDate,
                    TypeOfSend = x.Type,
                    SendStatus = x.SendStatus.Value,

                    //SubscribedGroups =
                    //    x.GroupSubscribers.Count(gs => !gs.CustomerGroup.IsDeleted)
                })
                .ToListAsync();

            return result;
        }

        public async Task SendNewsLetterAsync(NewsLetters newsLetter, NotificationObject notificationObject)
        {
            try
            {
                var NewsLetterEmailSetting = new EmailsSettings();

                if (newsLetter.EmailsSettingsId != null)
                {
                    NewsLetterEmailSetting = await _EmailsSettingsRepo.Query().Where(x => x.Id == newsLetter.EmailsSettingsId).FirstOrDefaultAsync();
                }
                else
                {
                    NewsLetterEmailSetting = await _EmailsSettingsRepo.Query().Where(x => x.IsDefault == true).FirstOrDefaultAsync();

                }

                var notificationResponse = new SendNotificationDto();

                // Email newsletters can target very large audiences (up to millions of
                // subscribers). Sending one message per recipient through the transactional
                // notifier would open a fresh SMTP connection per email, so instead we hand
                // the whole list to the bulk sender: it reuses one connection per batch,
                // throttles between batches, and still sends every user their own email
                // (nobody sees the other recipients in the "To" field).
                if (newsLetter.NotificationType == CoreNotificationType.Email
                    && notificationObject.SendValue != null
                    && notificationObject.SendValue.Count > 0)
                {
                    var (successCount, failureCount) = await _emailSender.SendBulkIndividualAsync(
                        notificationObject.SendValue,
                        notificationObject.EmailOptions?.SenderModule,
                        notificationObject.MessageTitle,
                        notificationObject.MessageBody,
                        newsLetter.SiteId,
                        NewsLetterEmailSetting,
                        isHtml: true,
                        attachment: notificationObject.Attachment,
                        CC: notificationObject.CC,
                        fileMemoryViewModels: notificationObject.Files);

                    notificationResponse.SuccessCount = successCount;
                    notificationResponse.FailureCount = failureCount;
                }
                else
                {
                    await _testModifier.SendMessageAsync((NotificationType)newsLetter.NotificationType, NewsLetterEmailSetting, notificationObject, newsLetter.SiteId);
                }

                await UpdateNewsletterStatistic(newsLetter.Id, notificationResponse);

                await ChangeSendStatus(newsLetter.Id, Entities.Enum.ESendStatus.SentSuccessfully);

            }
            catch (Exception ex)
            {
                await ChangeSendStatus(newsLetter.Id, Entities.Enum.ESendStatus.Faild);
            }
        }
        public async Task<NotificationObject> CreateNotificationObject(NewsLetters newsLetter)
        {
            NotificationObject notificationObj = new NotificationObject();

            EmailTemplateVM objCustomerEmailTemplate = await GetEmailTemplate(newsLetter.EmailTemplateId);

            await SetCommonProperties(notificationObj, objCustomerEmailTemplate, newsLetter);

            if (newsLetter.NotificationType == CoreNotificationType.SMS)
            {
                await SetSMSProperties(notificationObj, newsLetter);
            }
            else if (newsLetter.NotificationType == CoreNotificationType.PushNotification)
            {
                return await CreatePushNotification(newsLetter);
            }

            return notificationObj;
        }
        public async Task DeleteAsync(long id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();

                var newsLetter = await _newsLettersRepo
                    .Query()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (newsLetter != null)
                {
                    newsLetter.Status = EStatus.Deleted;
                    await _newsLettersRepo.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {

                throw new Exception("An error occurred while deleting the newsletter.", ex);
            }
        }
        public async Task<bool> AddSubscriber(long PageId, string ComponentSettingsId, EditUserDto User)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var user = await UpdateUser(User);
                var Settings = await _Pages.GetComponentsSettings(PageId, ComponentSettingsId, null, null, false);

                if (Settings == null) return false;

                var (newsLetterId, isEmailChecked, formSubmitter, adminEmailTemplateId, adminEmails, customerEmailTemplateId) = ExtractSettings(Settings);

                var newsLetterSubscribers = await _newsLetterSubscribersRepo
                    .Query()
                    .Where(x => x.NewsLetterId == newsLetterId && user.Id == x.UserId)
                    .ToListAsync();

                if (newsLetterSubscribers != null)
                {
                    _newsLetterSubscribersRepo.RemoveRange(newsLetterSubscribers);
                    await _newsLetterSubscribersRepo.SaveChangesAsync();
                }

                var emailSettings = await GetEmailSettings(newsLetterId, SiteId);

                if (isEmailChecked)
                {
                    var emails = new List<string> { user.Email };

                    var adminNotification = adminEmails != null ? await SendSubscriber(adminEmailTemplateId, adminEmails.ToObject<List<string>>()) : null;

                    if (formSubmitter)
                    {
                        var customerNotification = await SendSubscriber(customerEmailTemplateId, emails);

                        await _testModifier.SendMessageAsync(NotificationType.Email, emailSettings, customerNotification, SiteId);
                        if (adminNotification != null)
                        {
                            await _testModifier.SendMessageAsync(NotificationType.Email, emailSettings, adminNotification, SiteId);
                        }
                    }
                }

                _newsLetterSubscribersRepo.Add(new NewsLetterSubscribers { NewsLetterId = newsLetterId.Value, UserId = user.Id });
                await _newsLetterSubscribersRepo.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<string> GenerateNoificationLink(NotificationLinkVM model)
        {
            try
            {
                if (model != null)
                {
                    return await GetBaseUrl(model.CultureId) + model.NotificationLink;
                }
                return null;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<PaginationResult<NewsLetterDto>> GetSubscribedNotifications(SmartTableParam model)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();

                var siteId = _workContext.GetCurrentSiteId();

                var newsLetterIds = _newsLetterSubscribersRepo.Query()
                                                              .Where(x => x.UserId == userId)
                                                              .Select(x => x.NewsLetterId)
                                                              .ToList();


                var userGroup = _customerGroupUser.Query()
                                              .Where(x => x.UserId == userId && !x.CustomerGroup.IsDeleted)
                                              .Select(x => x.CustomerGroupId)
                                              .ToList();



                var newsLetters = _newsLettersRepo
                                                 .Query()
                                                 .Where(x => x.SiteId == siteId && x.Status == EStatus.Published && (newsLetterIds.Contains(x.Id) || x.GroupSubscribers.Any(y => userGroup.Contains(y.CustomerGroupId))))
                                                 .AsNoTracking();





                if (!newsLetters.Any())
                {
                    return new PaginationResult<NewsLetterDto>(new List<NewsLetterDto>(), 0, 0);
                }

                var result = await newsLetters.Select(y => new NewsLetterDto
                {
                    Id = y.Id,
                    Name = y.Name,
                    Description = y.Description,
                    IsMuted = y.NewsLetterSubscribers.Any(x => x.IsMuted && (x.UserId == userId)),

                }).PaginateAsync(model.Pagination.Number, model.Pagination.Start);

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> MuteSubcribedNotifications(long Id)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();

                var subscribedNotifications = _newsLetterSubscribersRepo
                                                         .Query()
                                                         .Where(x => x.NewsLetterId == Id && x.UserId == userId && (x.NewsLetter.Status == EStatus.Published))
                                                         .FirstOrDefault();





                if (subscribedNotifications != null)
                {
                    subscribedNotifications.IsMuted = true;

                }
                else
                {
                    var subscribedNotificationToBeadded = new NewsLetterSubscribers { NewsLetterId = Id, UserId = userId, IsMuted = true };
                    _newsLetterSubscribersRepo.Add(subscribedNotificationToBeadded);
                }

                _newsLetterSubscribersRepo.SaveChanges();


                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> UnMuteSubcribedNotifications(long Id)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();

                var subscribedNotifications = _newsLetterSubscribersRepo
                                                         .Query()
                                                         .Where(x => x.NewsLetterId == Id && x.UserId == userId && x.IsMuted)
                                                         .Include(x => x.User)
                                                         .Where(x => x.User.Status != EStatus.Deleted && x.NewsLetter.Status == EStatus.Published)
                                                         .FirstOrDefault();


                if (subscribedNotifications != null)
                {
                    subscribedNotifications.IsMuted = false;
                    _newsLetterSubscribersRepo.SaveChanges();
                    return true;

                }



                return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> UpdateNewsletterViewerCount(long Id)
        {
            try
            {


                var newsletterStatistics = _newsLetterStatisticsRepo.Query()
                                                                   .Where(x => x.NewsLetterId == Id)
                                                                   .OrderByDescending(x => x.CreationDate)
                                                                   .FirstOrDefault();


                if (newsletterStatistics != null)
                {
                    var userId = await _workContext.GetCurrentUserId();

                    if (_newsLetterViwerRepo.Query().Any(x => x.UserId == userId && x.NewsLetterStatisticId == newsletterStatistics.Id))
                    {
                        return false;
                    }




                    var newsletterViwer = new NewsLetterViwers { CreationDate = DateTimeOffset.Now, UserId = userId, NewsLetterStatisticId = newsletterStatistics.Id };

                    _newsLetterViwerRepo.Add(newsletterViwer);
                    _newsLetterViwerRepo.SaveChanges();

                }


                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        #region Helper Functions

        private async Task<bool> ChangeSendStatus(long newsLettersId, Entities.Enum.ESendStatus? eSendStatus)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    var _UserVisitRepo = scope.ServiceProvider.GetRequiredService<IRepository<NewsLetters>>();
                    var query = await _UserVisitRepo.Query().Where(x => x.Id == newsLettersId).FirstOrDefaultAsync();


                    query.SendStatus = eSendStatus.Value;
                    _UserVisitRepo.Update(query);
                    await _UserVisitRepo.SaveChangesAsync();

                    return true;
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }
        }
        private async Task<List<UsersNewsLetter>> GetUsers(long newsletterId)
        {
            try
            {

                var Users = await _newsLetterSubscribersRepo
                                                              .Query()
                                                              .Where(x => x.NewsLetterId == newsletterId)
                                                              .Include(x => x.User)
                                                              .Select(y => new UsersNewsLetter
                                                              {
                                                                  Id = y.User.Id,
                                                                  UserName = y.User.UserName,
                                                                  Email = y.User.Email
                                                              })
                                                              .AsNoTracking()
                                                              .ToListAsync();




                return Users;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<NewsLetters>> GetNewsLetters()
        {
            var Newsletters = await _newsLettersRepo
                                                  .Query()
                                                  .Where(x => x.Status != EStatus.Deleted && x.Status != EStatus.Unpublished
                                                  && x.Type == Entities.Enum.NewsLetterType.Scheduled)
                                                  .Include(x => x.NewsLetterSubscribers)
                                                  .ThenInclude(x => x.User)
                                                  .AsNoTracking()
                                                  .ToListAsync();

            return Newsletters;
        }
        private async Task<User> UpdateUser(EditUserDto User)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();
                var user = _userrepository.Query().Where(x => x.Id == userId).FirstOrDefault();

                var UserByEmail = await _userrepository.Query().Where(x => x.Email == User.Email).FirstOrDefaultAsync();

                if (UserByEmail is null || UserByEmail.Id == userId || user.Type == EUsersTypes.GuestClient)
                {
                    user.PhoneNumber = string.IsNullOrEmpty(User.PhoneNumber) != true ? User.PhoneNumber : user.PhoneNumber;
                    user.FullName = string.IsNullOrEmpty(User.FullName) != true ? User.FullName : user.FullName;
                    user.Email = User.Email;
                }


                await _userrepository.SaveChangesAsync();
                return user;


            }
            catch (Exception ex)
            {

                throw ex;
            }


        }
        private async Task<List<FileViewModelDto>> GetMedias(long newsLetterId)
        {
            try
            {


                var query = await _newsLetterMediasRepo
                    .Query()
                    .Include(x => x.Media)
                    .Where(x => x.NewsLetterId == newsLetterId)
                    .Select(x => new FileViewModelDto
                    {
                        Id = x.Media.Id,
                        publicUrl = x.Media.PublicUrl,
                        contentType = x.Media.ContentType,
                        fileName = x.Media.Filename,
                    })
                    .AsNoTracking()
                    .ToListAsync();

                return query;
            }
            catch (Exception)
            {

                throw;
            }
        }
        private async Task<bool> SendMessage(NewsLetters newsLetters)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    INewsLetterService newsLetterService = scope.ServiceProvider.GetRequiredService<INewsLetterService>();

                    NotificationObject Notificationobj = await newsLetterService.CreateNotificationObject(newsLetters);
                    if (Notificationobj != null)
                    {
                        await newsLetterService.SendNewsLetterAsync(newsLetters, Notificationobj);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private async Task<NotificationObject> SendSubscriber(string EmailTemplateId, List<string> Email)
        {
            NotificationObject Notificationobj = new NotificationObject();

            EmailTemplateVM objCustomerEmailTemplate = await _emailTemplateService.GetEmailTemplateById(EmailTemplateId);

            Notificationobj.MessageTitle = objCustomerEmailTemplate.Subject;

            Notificationobj.MessageBody = objCustomerEmailTemplate.EmailBody;

            Notificationobj.SendValue = Email;

            Notificationobj.Attachment = null;

            Notificationobj.Severity = 0;

            Notificationobj.EmailOptions.TemplateId = objCustomerEmailTemplate?.Id != null ? objCustomerEmailTemplate.Id : null;

            return Notificationobj;

        }

        private Entities.Enum.NewsLetterType ChangeStatusType(NewsLetters newsLetter, bool? IsSend)
        {
            try
            {

                switch (newsLetter.Type)
                {
                    case Entities.Enum.NewsLetterType.Draft when IsSend.Value:
                        newsLetter.Type = Entities.Enum.NewsLetterType.Sent;
                        break;

                    case Entities.Enum.NewsLetterType.Sent when IsSend.Value:
                        newsLetter.Type = Entities.Enum.NewsLetterType.Resent;
                        break;

                    case Entities.Enum.NewsLetterType.Scheduled when newsLetter.sendTimer == Entities.Enum.ESendTimer.OneTime:
                        newsLetter.Type = Entities.Enum.NewsLetterType.Scheduled;
                        break;

                    case Entities.Enum.NewsLetterType.Sent when newsLetter.sendTimer == Entities.Enum.ESendTimer.OneTime:
                    case Entities.Enum.NewsLetterType.Resent when newsLetter.sendTimer == Entities.Enum.ESendTimer.OneTime:
                        newsLetter.Type = Entities.Enum.NewsLetterType.Rescheduled;
                        break;

                    case Entities.Enum.NewsLetterType.Scheduled when newsLetter.sendTimer == Entities.Enum.ESendTimer.Yearly ||
                                                                    newsLetter.sendTimer == Entities.Enum.ESendTimer.Daily ||
                                                                    newsLetter.sendTimer == Entities.Enum.ESendTimer.Monthly:
                        newsLetter.Type = Entities.Enum.NewsLetterType.Scheduled;
                        break;
                }

                return newsLetter.Type;

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        private async Task<EmailTemplateVM> GetEmailTemplate(string emailTemplateId)
        {
            return !string.IsNullOrEmpty(emailTemplateId)
                ? await _emailTemplateService.GetEmailTemplateById(emailTemplateId.ToString())
                : null;
        }

        private async Task SetCommonProperties(NotificationObject notificationObj, EmailTemplateVM emailTemplate, NewsLetters newsLetter)
        {
            var medias = await GetMedias(newsLetter.Id);
            notificationObj.MessageTitle = emailTemplate != null ? emailTemplate.Subject : newsLetter.Subject;
            notificationObj.MessageBody = emailTemplate != null ? emailTemplate.EmailBody : newsLetter.NewsLetterBody;
            notificationObj.Attachment = medias.Select(x => new FileViewModel
            {
                contentType = x.contentType,
                fileName = x.fileName,
                publicUrl = x.publicUrl,

            }).ToList();
            notificationObj.Severity = 0;
            notificationObj.EmailOptions.TemplateId = emailTemplate?.Id;

            if (newsLetter.NotificationType == CoreNotificationType.Email)
            {

                var allusers = await NewsLetterStatisticsHelper(newsLetter);

                notificationObj.SendValue = allusers.Where(x => !x.IsMuted && !string.IsNullOrEmpty(x.Email))
                                                    .DistinctBy(x => x.Email)
                                                    .Select(x => x.Email)
                                                    .ToList();
            }
        }

        private async Task SetSMSProperties(NotificationObject notificationObj, NewsLetters newsLetter)
        {
            var allusers = await NewsLetterStatisticsHelper(newsLetter);

            notificationObj.SendValue = allusers.Where(x => !x.IsMuted && !string.IsNullOrEmpty(x.PhoneNumber))
                                                .DistinctBy(x => x.PhoneNumber)
                                                .Select(x => x.PhoneNumber)
                                                .ToList();

            var smsProvider = await siteSettingsRepository.Query()
                .Where(x => x.Id == "SmsProvider" && x.SiteId == newsLetter.SiteId)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();

            notificationObj.SMSProvider = smsProvider == null
                ? SMSProviders.SMSUniFonic
                : (SMSProviders)Enum.Parse(typeof(SMSProviders), smsProvider, true);
        }

        private async Task<NotificationObject> CreatePushNotification(NewsLetters newsLetter)
        {
            var additionalData = new Dictionary<string, string>();

            additionalData.Add("NewsLetterId", newsLetter.Id.ToString());

            if (!string.IsNullOrEmpty(newsLetter.NotificationLink))
            {
                additionalData.Add("Link", newsLetter.NotificationLink);

            }

            if (newsLetter.NewsLetterMedias.Any())
            {
                var targetImage = newsLetter.NewsLetterMedias.FirstOrDefault();
                additionalData.Add("Image", targetImage.Media.PublicUrl);
                additionalData.Add("ImageContentType", targetImage.Media.ContentType);

            }
            NotificationObject notificationObjPush = new NotificationObject
            {
                SendVia = SendBy.Username,
                MessageBody = newsLetter.NewsLetterBody,
                MessageTitle = newsLetter.Subject,

                NotificationType = NotificationType.PushNotification,
                Attachment = newsLetter.NewsLetterMedias.Select(x => new FileViewModel
                {
                    contentType = x.Media.ContentType,
                    publicUrl = x.Media.PublicUrl,

                }).ToList(),
                PushNotificationOptions = new PushNotification
                {
                    Topics = new List<string>(),
                    DesitinationType = DesitinationType.Personal,
                    Data = additionalData,
                }
            };

            var allusers = await NewsLetterStatisticsHelper(newsLetter);

            notificationObjPush.SendValue = allusers.Where(x => !x.IsMuted && !string.IsNullOrEmpty(x.DeviceToken))
                                                    .DistinctBy(x => x.DeviceToken)
                                                    .Select(x => x.DeviceToken)
                                                    .ToList();

            notificationObjPush.UserId = allusers.Where(x => !x.IsMuted && !string.IsNullOrEmpty(x.DeviceToken))
                                                  .Select(x => x.Id)
                                                  .ToArray();


            return notificationObjPush;
        }

        private (long? newsLetterId, bool isEmailChecked, bool formSubmitter, string adminEmailTemplateId, JArray adminEmails, string customerEmailTemplateId) ExtractSettings(Dictionary<string, object> settings)
        {
            settings.TryGetValue("NewsLetterId", out var newsLetterId);
            settings.TryGetValue("IsEmailChecked", out var isEmailChecked);
            settings.TryGetValue("formSubmitter", out var formSubmitter);
            settings.TryGetValue("TemplateEmailId", out var adminEmailTemplateId);
            settings.TryGetValue("emailsTo", out var adminEmails);
            settings.TryGetValue("formReciveSubmiterMessage", out var customerEmailTemplateId);

            return (string.IsNullOrEmpty(newsLetterId?.ToString()) ? null : Convert.ToInt64(newsLetterId),
                    Convert.ToBoolean(Convert.ToInt64(isEmailChecked)),
                    Convert.ToBoolean(formSubmitter?.ToString()),
                    Convert.ToString(adminEmailTemplateId),
                    adminEmails as JArray,
                    Convert.ToString(customerEmailTemplateId));
        }

        private async Task<EmailsSettings> GetEmailSettings(long? newsLetterId, long SiteId)
        {
            var emailSettings = await _newsLettersRepo
                .Query()
                .Where(x => x.Id == newsLetterId)
                .Include(x => x.EmailsSettings)
                .Select(x => x.EmailsSettings)
                .FirstOrDefaultAsync()
                ?? await _EmailsSettingsRepo
                    .Query()
                    .Where(x => x.IsDefault == true && x.SiteId == SiteId)
                    .FirstOrDefaultAsync();

            return emailSettings;
        }

        private static void ApplySearchTextFilter(ref List<NewsLetterResultVm> query, string searchText)
        {
            if (!string.IsNullOrEmpty(searchText))
            {
                var inputWithTrim = searchText.ToLower().Trim();
                query = query
                    .Where(x => x.Name.ToLower().Contains(inputWithTrim))
                    .ToList();
            }
        }

        private static void ApplyNotificationTypeFilter(ref List<NewsLetterResultVm> query, CoreNotificationType? notificationType)
        {
            if (notificationType.HasValue)
            {
                query = query
                    .Where(x => x.NotificationType == notificationType.Value)
                    .ToList();
            }
        }



        public async Task<bool> UpdateNewsletterStatistic(long Id, SendNotificationDto model)
        {
            try
            {


                var newsletterStatistics = _newsLetterStatisticsRepo.Query()
                                                                   .Where(x => x.NewsLetterId == Id)
                                                                   .OrderByDescending(x => x.CreationDate)
                                                                   .FirstOrDefault();


                if (newsletterStatistics != null)
                {
                    var userId = await _workContext.GetCurrentUserId();

                    if (_newsLetterViwerRepo.Query().Any(x => x.UserId == userId && x.NewsLetterStatisticId == newsletterStatistics.Id))
                    {
                        return false;
                    }

                    var formatDate = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    var query = $" update newsletter_newsletterstatistics set SuccessCount = {model.SuccessCount}, FailedNotificationCount = FailedNotificationCount + {model.FailureCount}, ModificationDate = '{formatDate}'  where Id = {newsletterStatistics.Id}";


                    var subscribedNotifications = _context.Database.ExecuteSqlRaw(query);



                }


                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Newsletter Upsert

        private async Task<NewsLetters> GetOrCreateNewsLetterAsync(RequestNewsLetterVm model, long siteId, bool isEditMode, long userId)
        {
            if (isEditMode)
            {
                var existingNewsLetter = await _newsLettersRepo
                                              .Query()
                                              .Where(m => m.Id == model.Id && m.SiteId == siteId)
                                              .Include(x => x.NewsLetterSubscribers)
                                              .ThenInclude(x => x.User)
                                              .Include(x => x.NewsLetterMedias)
                                              .FirstOrDefaultAsync();

                if (existingNewsLetter == null)
                {
                    throw new Exception("Not Found NewsLetter");
                }

                existingNewsLetter.ModifiedById = userId;
                existingNewsLetter.ModificationDate = DateTimeOffset.Now;

                ValidateSendTimerChange(existingNewsLetter, model.sendTimer);

                existingNewsLetter.Type = ChangeStatusType(existingNewsLetter, model.IsSend);

                return existingNewsLetter;
            }

            return new NewsLetters
            {
                Type = model.Type,
                CreatedById = userId,
                CreationDate = DateTimeOffset.Now,
                sendTimer = model.sendTimer
            };
        }

        private async Task UpdateNewsLetterProperties(NewsLetters newsLetter, RequestNewsLetterVm model, long siteId)
        {
            newsLetter.Name = model.Name;
            newsLetter.Description = model.Description;
            newsLetter.EmailsSettingsId = model.SenderEmailId;
            newsLetter.NotificationType = model.NotificationType;
            newsLetter.SentDate = model.SentDate;
            newsLetter.EmailTemplateId = model.EmailTemplateId;
            newsLetter.EmailTemplateSiteId = string.IsNullOrEmpty(model.EmailTemplateId) ? null : (long?)siteId;
            newsLetter.Subject = model.Subject;
            newsLetter.Status = model.Status;
            newsLetter.SiteId = siteId;
            newsLetter.NewsLetterBody = model.NewsLetterBody;
            newsLetter.SendStatus = Entities.Enum.ESendStatus.Pending;
            newsLetter.EndDate = model.EndDate;
            if (model.notificationLinkModel != null)
            {
                await AddNoificationLink(model.notificationLinkModel, newsLetter);
            }
            else
            {
                newsLetter.NotificationLink = null;
                newsLetter.IsExternalLink = false;
            }
        }

        private async Task AddNoificationLink(NotificationLinkVM notificationLinkModel, NewsLetters newsLetter)
        {
            try
            {
                if (!string.IsNullOrEmpty(notificationLinkModel.NotificationLink))
                {

                    newsLetter.NotificationLink = await GetBaseUrl(notificationLinkModel.CultureId) + notificationLinkModel.NotificationLink;
                    newsLetter.IsExternalLink = false;

                }
                else if (!string.IsNullOrEmpty(notificationLinkModel.ExternalLink))
                {
                    newsLetter.NotificationLink = notificationLinkModel.ExternalLink;
                    newsLetter.IsExternalLink = true;
                }


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<string> GetBaseUrl(long? cultureId)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var SiteHostName = await sitesHostnameRepository.Query().AsNoTracking().Where(x => x.CultureId == cultureId)
                                                                  .Where(x => x.SiteId == siteId)
                                                                  .FirstOrDefaultAsync();
                if (string.IsNullOrEmpty(SiteHostName.Prefix))
                {
                    return "https://" + SiteHostName.HostName + "/";
                }
                else
                {
                    return "https://" + SiteHostName.HostName + "/" + SiteHostName.Prefix + "/";
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void ValidateSendTimerChange(NewsLetters newsLetter, Entities.Enum.ESendTimer sendTimer)
        {
            if (newsLetter.sendTimer == Entities.Enum.ESendTimer.None)
            {
                if (newsLetter.sendTimer != sendTimer)
                {
                    throw new Exception("Can Not Change From SendNow To Another ");
                }
            }
            else if (newsLetter.sendTimer == Entities.Enum.ESendTimer.OneTime || newsLetter.sendTimer == Entities.Enum.ESendTimer.Yearly ||
                     newsLetter.sendTimer == Entities.Enum.ESendTimer.Daily || newsLetter.sendTimer == Entities.Enum.ESendTimer.Monthly)
            {
                if (sendTimer == Entities.Enum.ESendTimer.None)
                {
                    throw new Exception("Can Not Change From Scheduled To SendNow ");
                }

                newsLetter.sendTimer = sendTimer;
            }
        }

        private async Task InsertUpdateNewsLetterSubscribersAsync(long newsLetterId, List<long>? subscribers)
        {
            if (subscribers != null)
            {
                var userSubscribers = await _newsLetterSubscribersRepo.Query().Where(x => x.NewsLetterId == newsLetterId && x.IsDirectSubscribed).ToListAsync();

                var newSubscribers = subscribers.Where(x => !userSubscribers.Any(y => y.UserId == x)).ToList();

                var removeableSubscribers = userSubscribers.Where(x => !subscribers.Contains(x.UserId)).ToList();

                var newsLetterSubscribers = newSubscribers.Select(userId => new NewsLetterSubscribers { NewsLetterId = newsLetterId, UserId = userId, IsDirectSubscribed = true }).ToList();
                _newsLetterSubscribersRepo.AddRange(newsLetterSubscribers);

                _newsLetterSubscribersRepo.RemoveRange(removeableSubscribers);
                await _newsLetterSubscribersRepo.SaveChangesAsync();
            }
        }

        private async Task UpsertNewsLetterGroupSubscribesAsync(long newsLetterId, List<long> groupIds)
        {
            var groupSubscribers = await _newsLetterGroupSubscribers.Query()
                                                                    .Where(x => x.NewsLetterId == newsLetterId)
                                                                    .Include(x => x.CustomerGroup)
                                                                    .ToListAsync();



            var newSubscribers = groupIds.Where(x => !groupSubscribers.Any(y => y.CustomerGroupId == x)).ToList();

            var removeableGroupSubscribers = groupSubscribers.Where(x => !groupIds.Contains(x.CustomerGroupId)).ToList();



            var newsLetterSubscribers = newSubscribers.Select(groupId => new NewsLetterGroupSubscribers { NewsLetterId = newsLetterId, CustomerGroupId = groupId }).ToList();
            _newsLetterGroupSubscribers.AddRange(newsLetterSubscribers);

            if (removeableGroupSubscribers.Any())
            {

                var groupIdss = removeableGroupSubscribers.Select(x => x.CustomerGroupId).ToList();
                var removableUserIds = await _userrepository.Query().Where(x => x.CustomerGroups.Any(y => groupIds.Contains(y.CustomerGroupId))).AsNoTracking().Select(x => x.Id).ToListAsync();


                //var removableUserIds = removeableGroupSubscribers.SelectMany(x => x.CustomerGroup.Users)
                //                                               .Select(x => x.UserId)
                //                                               .ToList();

                var removableSubscribers = await _newsLetterSubscribersRepo.Query().Where(x => removableUserIds.Contains(x.UserId) && !x.IsDirectSubscribed).ToListAsync();

                _newsLetterSubscribersRepo.RemoveRange(removableSubscribers);
                await _newsLetterSubscribersRepo.SaveChangesAsync();

                _newsLetterGroupSubscribers.RemoveRange(removeableGroupSubscribers);
            }

            await _newsLetterGroupSubscribers.SaveChangesAsync();
        }

        private async Task InsertUpdateNewsLetterMediasAsync(long newsLetterId, List<long>? uploadIds)
        {
            var mediaNewsLetter = await _newsLetterMediasRepo.Query().Where(x => x.NewsLetterId == newsLetterId).ToListAsync();
            _newsLetterMediasRepo.RemoveRange(mediaNewsLetter);
            if (uploadIds.Count > 0)
            {
                var newsLetterMedias = uploadIds.Select(id => new NewsLetterMedias { NewsLetterId = newsLetterId, MediaId = id }).ToList();
                _newsLetterMediasRepo.AddRange(newsLetterMedias);
            }
            await _newsLetterMediasRepo.SaveChangesAsync();
        }

        private bool ShouldSendMessage(NewsLetters newsLetter, RequestNewsLetterVm model)
        {
            return (newsLetter.Type == Entities.Enum.NewsLetterType.Sent || newsLetter.Type == Entities.Enum.NewsLetterType.Resent) &&
                   newsLetter.sendTimer == Entities.Enum.ESendTimer.None && model.IsSend == true;
        }

        private async Task<List<SubscribedUsers>> NewsLetterStatisticsHelper(NewsLetters newsLetter)
        {

            var userGroup = _customerGroup.Query().Where(x => x.SubscribedNewsLetters.Any(y => y.NewsLetterId == newsLetter.Id) && !x.IsDeleted)
                                                   .Include(x => x.Users)
                                                   .Include("Users.User")
                                                   .SelectMany(x => x.Users)
                                                   .Select(x => x.User)
                                                   .AsNoTracking()
                                                   .ToList();


            var subscribedUsers = newsLetter.NewsLetterSubscribers.Select(x => x.User).ToList();

            var allTargetedUsers = userGroup.Concat(subscribedUsers)
                                            .Where(x => x.Status != EStatus.Deleted)
                                            .DistinctBy(x => x.Id)
                                            .ToList();

            var allUsers = allTargetedUsers.Select(x => new SubscribedUsers
            {
                Id = x.Id,
                PhoneNumber = x.PhoneNumber,
                IsMuted = newsLetter.NewsLetterSubscribers.Any(y => y.IsMuted && y.UserId == x.Id),
                DeviceToken = x.DeviceToken,
                Email = x.Email,
                UserName = x.UserName,
            }).ToList();




            int failedCount = 0;
            int MutedCount = 0;
            int expectedRecipientCount = 0;


            switch (newsLetter.NotificationType)
            {

                case CoreNotificationType.SMS:
                    expectedRecipientCount = allUsers.Where(x => !string.IsNullOrEmpty(x.PhoneNumber)).Count();
                    MutedCount = allUsers.Where(x => x.IsMuted && !string.IsNullOrEmpty(x.PhoneNumber)).Count();
                    failedCount = allUsers.Where(x => string.IsNullOrEmpty(x.PhoneNumber)).Count();
                    break;
                case CoreNotificationType.Email:
                    expectedRecipientCount = allUsers.Where(x => !string.IsNullOrEmpty(x.Email)).Count();
                    MutedCount = allUsers.Where(x => x.IsMuted && !string.IsNullOrEmpty(x.Email)).Count();
                    failedCount = allUsers.Where(x => string.IsNullOrEmpty(x.Email)).Count();
                    break;
                case CoreNotificationType.PushNotification:
                    expectedRecipientCount = allUsers.Where(x => !string.IsNullOrEmpty(x.DeviceToken)).Count();
                    MutedCount = allUsers.Where(x => x.IsMuted && !string.IsNullOrEmpty(x.DeviceToken)).Count();
                    break;
                default:
                    break;
            }



            var newsLetterStatistics = new NewsLetterStatistics
            {
                NewsLetterId = newsLetter.Id,
                ExpectedRecipientCount = expectedRecipientCount,
                CreationDate = DateTimeOffset.Now,
                FailedNotificationCount = failedCount,
                MutedUserCount = MutedCount,
            };

            _newsLetterStatisticsRepo.Add(newsLetterStatistics);
            _newsLetterStatisticsRepo.SaveChanges();


            return allUsers;

        }

        #endregion
    }
}
