using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Achievements;
using EskaCMS.Entities.Entity.Chat;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Forms;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.Achievements.Enums.AchievmentEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Engine.UserAchievement.BGS
{
    public class UserAchievementBackgroundService : BaseBackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRepository<Message> _messageRepository;
        private readonly IRepository<UserLoginTransactions> _userLoginTransactionsRepository;

        private readonly IRepository<UserAchievments> _userachievementsRepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> _siteSettingsRepository;
        private readonly IRepositoryWithCustomId<CustomerGroupUser> _customerGroupUserRepository;
        private readonly IRepository<SubmittedForm> _submittedFormRepository;
        public readonly ILogger<UserAchievementBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IdcmsLogService dcmsLogService;


        public UserAchievementBackgroundService(IServiceProvider serviceProvider
           , ILogger<UserAchievementBackgroundService> logger
           , IConfiguration configuration)
           : base(nameof(UserAchievementBackgroundService), logger, serviceProvider, configuration)
        {

            _serviceProvider = serviceProvider;

            _logger = logger;

            _configuration = configuration;


        }

        public override async Task<bool> Execute()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                // Resolve the logger BEFORE the try so the catch block below can use it.
                // Previously the local was declared inside the try, so the catch bound to the
                // never-assigned class field (null) and threw an NRE that masked the real error.
                var dcmsLogService = scope.ServiceProvider.GetRequiredService<IdcmsLogService>();

                try
                {

                    await dcmsLogService.AddInfo("UserAchievement BackgroundService", "UserAchievement BackgroundServicee Start", "", null);

                    var _achievementRepository = scope.ServiceProvider.GetRequiredService<IRepository<Achievement>>();

                    var allActiveAchievements = _achievementRepository.Query()
                                                                  .Where(x => x.Status == EStatus.Published)
                                                                  .AsNoTracking()
                                                                  .ToList();

                    foreach (var achievement in allActiveAchievements)
                    {
                        switch (achievement.Type)
                        {
                            case AchievmentType.Logins:
                                await HandleLoginsAchievement(achievement, scope);
                                break;
                            case AchievmentType.Messages:
                                await HandleMessagesAchievement(achievement, scope);
                                break;
                            case AchievmentType.ChatRooms:
                                await HandleChatRoomsAchievement(achievement, scope);
                                break;
                            case AchievmentType.SubmitData:
                                await HandleSubmitDataAchievement(achievement, scope);
                                break;
                            //case AchievmentType.CompleteProfileInfo:
                            //    await HandleCompleteProfileInfoAchievement(achievement);
                            //    break;
                            //case AchievmentType.PostComments:
                            //    await HandlePostCommentsAchievement(achievement);
                            //    break;
                            //case AchievmentType.ProductComments:
                            //    await HandleProductCommentsAchievement(achievement);
                            //    break;
                            default:

                                break;
                        }


                    }


                }
                catch (Exception e)
                {
                    await dcmsLogService.AddError("UserAchievement BackgroundService", "", e.Message, "", null);

                }
                return true;



            }


        }

        private async Task<bool> HandleLoginsAchievement(Achievement achievment, IServiceScope scope)
        {

            var _userLoginTransactionsRepository = scope.ServiceProvider.GetRequiredService<IRepository<UserLoginTransactions>>();
            switch (achievment.ObtainType)
            {
                case AchievementObtainType.Overall:

                    var query = await _userLoginTransactionsRepository.Query()
                                                                .Where(x => x.UserSite.SiteId == achievment.SiteId && x.UserSite.User.Status == EStatus.Active && x.LoginDate >= achievment.CreationDate && x.LoggingType == UserLoggingType.LogginSuccess)
                                                                .Include(x => x.UserSite)
                                                                .ThenInclude(x => x.User)
                                                                .GroupBy(x => x.UserSiteId)
                                                                .Select(x => new { x.FirstOrDefault().UserSite.UserId, UserTransaction = x.ToList() })
                                                                .ToListAsync();

                    var userIds = query.Where(x => x.UserTransaction.Count >= achievment.ExceededCount).Select(x => x.UserId).ToList();

                    if (userIds.Any())
                    {
                        await InsertUserAchievemetns(userIds, achievment.Id, scope);
                    }

                    break;
                case AchievementObtainType.Daily:

                    query = await _userLoginTransactionsRepository.Query()
                                                            .Where(x => x.UserSite.SiteId == achievment.SiteId && x.UserSite.User.Status == EStatus.Active && x.LoginDate.Date == DateTimeOffset.Now.Date && x.LoginDate >= achievment.CreationDate && x.LoggingType == UserLoggingType.LogginSuccess)
                                                            .Include(x => x.UserSite)
                                                            .ThenInclude(x => x.User)
                                                            .GroupBy(x => x.UserSiteId)
                                                            .Select(x => new { x.FirstOrDefault().UserSite.UserId, UserTransaction = x.ToList() })
                                                            .ToListAsync();

                    userIds = query.Where(x => x.UserTransaction.Count >= achievment.ExceededCount).Select(x => x.UserId).ToList();

                    if (userIds.Any())
                    {
                        await InsertUserAchievemetns(userIds, achievment.Id, scope);
                    }

                    break;

            }

            return true;

        }

        private async Task<bool> HandleMessagesAchievement(Achievement achievment, IServiceScope scope)
        {

            var _messageRepository = scope.ServiceProvider.GetRequiredService<IRepository<Message>>();
            switch (achievment.ObtainType)
            {
                case AchievementObtainType.Overall:

                    var query = await _messageRepository.Query()
                                                                .Where(x => x.SiteId == achievment.SiteId && x.Sender.Status == EStatus.Active && x.MessageSent >= achievment.CreationDate)
                                                                .Include(x => x.Sender)
                                                                .GroupBy(x => x.SenderId)
                                                                .Select(x => new { UserId = x.Key, Messages = x.ToList() })
                                                                .ToListAsync();

                    var userIds = query.Where(x => x.Messages.Count >= achievment.ExceededCount).Select(x => x.UserId).ToList();

                    if (userIds.Any())
                    {
                        await InsertUserAchievemetns(userIds, achievment.Id, scope);
                    }

                    break;
                case AchievementObtainType.Daily:

                    query = query = await _messageRepository.Query()
                                                                .Where(x => x.SiteId == achievment.SiteId && x.Sender.Status == EStatus.Active && x.MessageSent.Date == DateTimeOffset.Now.Date && x.MessageSent >= achievment.CreationDate)
                                                                .Include(x => x.Sender)
                                                                .GroupBy(x => x.SenderId)
                                                                .Select(x => new { UserId = x.Key, Messages = x.ToList() })
                                                                .ToListAsync();

                    userIds = query.Where(x => x.Messages.Count >= achievment.ExceededCount).Select(x => x.UserId).ToList();

                    if (userIds.Any())
                    {
                        await InsertUserAchievemetns(userIds, achievment.Id, scope);
                    }

                    break;

            }

            return true;
        }
        private async Task<bool> HandleChatRoomsAchievement(Achievement achievment, IServiceScope scope)
        {
            var _siteSettingsRepository = scope.ServiceProvider.GetRequiredService<IRepositoryWithTypedId<SiteSettings, string>>();
            var _customerGroupUserRepository = scope.ServiceProvider.GetRequiredService<IRepositoryWithCustomId<CustomerGroupUser>>();
            // Resolve the logger from the scope (the class field of the same name is never assigned -> would NRE).
            var dcmsLogService = scope.ServiceProvider.GetRequiredService<IdcmsLogService>();
            var settings = _siteSettingsRepository.Query()
                                                     .Where(x => x.Id == "GroupTypeId" && x.SiteId == achievment.SiteId)
                                                     .FirstOrDefault();
            if (settings is null)
            {
                await dcmsLogService.AddError("User Achievement BackgroundService", "There is no group type id in seeding data", "", "", achievment.SiteId);
                return false;
            }

            var groupTypeId = Convert.ToInt64(settings.Value);



            var query = await _customerGroupUserRepository.Query()
                                                        .Where(x => x.CustomerGroup.GroupTypeId == groupTypeId &&
                                                                    x.User.UserSites.Any(x => x.SiteId == achievment.SiteId) &&
                                                                    x.User.Status == EStatus.Active &&
                                                                    x.CreationDate >= achievment.CreationDate &&
                                                                    x.CustomerGroup.SiteId == achievment.SiteId)
                                                        .GroupBy(x => x.UserId)
                                                        .Select(x => new { UserId = x.Key, Groups = x.ToList() })
                                                        .ToListAsync();

            var userIds = query.Where(x => x.Groups.Count >= achievment.ExceededCount).Select(x => x.UserId).ToList();


            if (userIds.Any())
            {
                await InsertUserAchievemetns(userIds, achievment.Id, scope);
            }

            return true;

        }

        private async Task<bool> HandleSubmitDataAchievement(Achievement achievment, IServiceScope scope)
        {

            var _submittedFormRepository = scope.ServiceProvider.GetRequiredService<IRepository<SubmittedForm>>();

            var numberOfDays = achievment.ExceededCount;
            var lastSevenDays = DateTimeOffset.Now.AddDays(-numberOfDays);
            var allSubmittedData = await _submittedFormRepository.Query()
                                                            .Where(x => x.SiteId == achievment.SiteId &&
                                                                        x.CreationDate >= achievment.CreationDate &&
                                                                      x.CreationDate >= lastSevenDays &&
                                                                      x.CreatedBy.Status == EStatus.Active)
                                                            .GroupBy(x => x.CreatedById)
                                                            .Select(x => new { UserId = x.Key, SubmittedData = x.ToList() })
                                                            .ToListAsync();

            var modifiedData = allSubmittedData.Select(x => new
            {
                x.UserId,
                SubmittedData = x.SubmittedData
                       .DistinctBy(y => y.CreationDate.Date)
                       .OrderBy(y => y.CreationDate)
                       .ToList()
            }).ToList();




            var userIds = new List<long>();


            foreach (var userSubmission in modifiedData)
            {
                var userHasSubmitted = true;
                if (userSubmission.SubmittedData.Count >= numberOfDays)
                {

                    for (int i = 1; i < userSubmission.SubmittedData.Count; i++)
                    {
                        if (userSubmission.SubmittedData[i].CreationDate.Date != userSubmission.SubmittedData[i - 1].CreationDate.Date.AddDays(1))
                        {
                            userHasSubmitted = false;
                            break;
                        }
                    }

                }
                else
                {
                    userHasSubmitted = false;
                }

                if (userHasSubmitted)
                {
                    userIds.Add(userSubmission.UserId);
                }
            }

            if (userIds.Any())
            {
                await InsertUserAchievemetns(userIds, achievment.Id, scope);
            }

            return true;
        }

        //private async Task HandleCompleteProfileInfoAchievement(Achievement achievment)
        //{

        //}

        //private async Task HandlePostCommentsAchievement(Achievement achievment)
        //{

        //}

        //private async Task HandleProductCommentsAchievement(Achievement achievment)
        //{

        //}


        private async Task<bool> InsertUserAchievemetns(List<long> userIds, long achievementId, IServiceScope scope)
        {

            var _userachievementsRepository = scope.ServiceProvider.GetRequiredService<IRepository<UserAchievments>>();

            var userHasAchievements = _userachievementsRepository.Query()
                                                                  .Where(x => x.AchievementId == achievementId)
                                                                  .Select(x => x.UserId)
                                                                  .ToList();
            userIds = userIds.Where(x => !userHasAchievements.Contains(x)).ToList();

            var newAchievements = userIds.Select(x => new UserAchievments { UserId = x, AchievementId = achievementId, CreationDate = DateTimeOffset.Now })
                                         .ToList();

            if (newAchievements.Any())
            {
                _userachievementsRepository.AddRange(newAchievements);
                await _userachievementsRepository.SaveChangesAsync();
            }

            return true;
        }





    }
}
