
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Achievements;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.UserAchievements.Models;
using EskaCMS.UserAchievements.Services.Interface;
using EskaCommerce.Notifications.Notifiers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.Achievements.Enums.AchievmentEnums;
using static EskaCMS.Entities.Enum.FriendshipEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.UserAchievements.Services
{
    public class AchievementServices : IUserAchievementsServices
    {

        private readonly IWorkContext _workContext;
        private readonly IRepository<User> _userrepository;
        private readonly IRepository<Achievement> _achievementrepository;
        private readonly IRepository<AchievementDetails> _achievemenDetailstrepository;
        private readonly IRepository<UserAchievments> _userAchievmentsRepository;
        private readonly ISiteSettingsService _siteSettings;
        private readonly ITestNotifier _testNotifier;

        public AchievementServices(IWorkContext workContext,

                                   IRepository<User> userrepository,
                                    ISiteSettingsService siteSettings,
                                    ITestNotifier testNotifier,
                                    IRepository<Achievement> achievementrepository,
                                    IRepository<AchievementDetails> achievemenDetailstrepository,
                                    IRepository<UserAchievments> userAchievmentsRepository
                                    )
        {
            _workContext = workContext;
            _userrepository = userrepository;
            _siteSettings = siteSettings;
            _testNotifier = testNotifier;
            _achievementrepository = achievementrepository;
            _achievemenDetailstrepository = achievemenDetailstrepository;
            _userAchievmentsRepository = userAchievmentsRepository;
        }


        public async Task<bool> ChangeAchievementStatus(long achievementId , EStatus status)
        {
  
            var siteId = _workContext.GetCurrentSiteId();

            var achievement = await GetById(achievementId, siteId);

            if (achievement is null)
            {
                throw new Exception("Achievement doesn't Exist");
            }

            achievement.Status = status;

            await _achievementrepository.SaveChangesAsync();

            return true;

        }

        public async Task<bool> CreateAchievement(CreateAchievementVM model)
        {
            try
            {
                var currentUserId = await _workContext.GetCurrentUserId();

                var siteId = _workContext.GetCurrentSiteId();

                var achievementTobeAdded = new Achievement
                {
                    SiteId = siteId,
                    CreatedById = currentUserId,
                    CreationDate = DateTimeOffset.Now,
                    ObtainType = model.ObtainType,
                    ExceededCount = model.ExceededCount,
                    Status = model.Status,
                    ThumbnailId = model.ThumbnailId,
                    Type = model.Type
                };

                _achievementrepository.Add(achievementTobeAdded);
                await _achievementrepository.SaveChangesAsync();

                if (model.AchievementDetails.Any())
                {
                    await CreateAchievementDetails(model.AchievementDetails, achievementTobeAdded.Id);
                }

                return true;
            }
            catch (Exception e)
            {

                throw;
            }
           
        
    }

        public async Task<bool> UpdateAchievement(AchievementUpdateVM model)
        {

            var siteId = _workContext.GetCurrentSiteId();
            var achievement = await GetById(model.Id, siteId);

            if(achievement is null)
            {
                throw new Exception("Achievement doesn't Exist");
            }

            achievement.ThumbnailId = model.ThumbnailId;
            achievement.Type = model.Type;
            achievement.Status=model.Status;
            achievement.ObtainType=model.ObtainType;
            achievement.ExceededCount=model.ExceededCount;


            if (achievement.AchievementDetails.Any())
            {
                await UpdateAchievementDetails(achievement.AchievementDetails, model.AchievementDetails, achievement.Id);

            }

            return true;

        }


        public async Task<PaginationResult<AchievementListVM>> GetAllAchivements(SmartTableParam model)
        {

            var siteId = _workContext.GetCurrentSiteId();
           
            var query = _achievementrepository.Query()
                                              .Where(x=> x.SiteId == siteId &&  x.Status!= EStatus.Deleted)
                                              .Include(x=>x.AchievementDetails)
                                              .Include(x=>x.Thumbnail)
                                              .AsNoTracking();

            if (model.Search.PredicateObject != null)
            {
                dynamic search = model.Search.PredicateObject;

                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        query = query.Where(x => x.CreationDate.Date >= after.Date);
                    }

                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        query = query.Where(x => x.CreationDate.Date <= before.Date);
                    }

                }

                if (search.Name != null)
                {

                    string Name = search.Name;
                    query = query.Where(x => x.AchievementDetails.Any(x=> x.Name.ToLower().Contains(Name.ToLower())));
                }

                if (search.Description != null)
                {

                    string Description = search.Description;
                    query = query.Where(x => x.AchievementDetails.Any(x => x.Description.ToLower().Contains(Description.ToLower())));
                }

                if (search.Description != null)
                {

                    string Description = search.Description;
                    query = query.Where(x => x.AchievementDetails.Any(x => x.Description.ToLower().Contains(Description.ToLower())));
                }

                if (search.Type != null)
                {

                    AchievmentType Type = search.Type;
                    query = query.Where(x => x.Type == Type);
                }

                if (search.Status != null)
                {

                    EStatus Status = search.Status;
                    query = query.Where(x => x.Status == Status);
                }

            }
            var result = await query.Select(x => new AchievementListVM
            {
                Id = x.Id,
                Type = x.Type,
                AchievementDetails = x.AchievementDetails.Select(x => new AchievementDetailsVM { Id = x.Id, Name = x.Name, Description = x.Description, CultureId = x.CultureId, CultureCode = x.Culture.CultureId }).ToList(),
                Image = x.ThumbnailId.HasValue ? x.Thumbnail.PublicUrl : null,
                ObtainType = x.ObtainType,
                ThumbnailId = x.ThumbnailId,
                ExceededCount = x.ExceededCount,
                Status = x.Status,
                CreationDate = x.CreationDate

            }).PaginateAsync(model.Pagination.Number, model.Pagination.Start);

            return result;

        }

        public async Task<PaginationResult<UserAchievementVM>> GetUserAchivements(SmartTableParam model)
        {

            var siteId = _workContext.GetCurrentSiteId();

            var userId = await _workContext.GetCurrentUserId();

            var cultureId = _workContext.GetCurrentCultureId();


            var query = _userAchievmentsRepository.Query()
                                           .Where(x => x.Achievement.SiteId == siteId && 
                                                       x.Achievement.Status == EStatus.Published && 
                                                       x.UserId == userId && 
                                                       x.Achievement.AchievementDetails.Any(x=>x.CultureId== cultureId))
                                           .Include(x => x.Achievement)
                                           .ThenInclude(x => x.Thumbnail)
                                           .AsNoTracking();

            var result = await query.Select(x => new UserAchievementVM
            {
                
                Name= x.Achievement.AchievementDetails.Where(x=>x.CultureId == cultureId).FirstOrDefault().Name,
                Description= x.Achievement.AchievementDetails.Where(x=>x.CultureId == cultureId).FirstOrDefault().Description,
                Image = x.Achievement.ThumbnailId.HasValue ? x.Achievement.Thumbnail.PublicUrl : null,
                CreationDate = x.CreationDate

            }).PaginateAsync(model.Pagination.Number, model.Pagination.Start);


            return result;
        }

        #region Helpers

        public async Task<bool> CreateAchievementDetails(List<AchievementDetailsVM> achievementDetails, long achievementId)
        {
            var achievementDetailsToBeAdded = achievementDetails.Select(x => new AchievementDetails
            {
                CultureId = x.CultureId,
                Description = x.Description,
                Name = x.Name,
                AchievementId = achievementId,
            }).ToList();
            _achievemenDetailstrepository.AddRange(achievementDetailsToBeAdded);
            await _achievemenDetailstrepository.SaveChangesAsync();
            return true;

        }

        public async Task<bool> UpdateAchievementDetails(IList<AchievementDetails> achievementDetailsList, List<AchievementDetailsVM> achievementInputDetails, long achievementId)
        {
            if (achievementDetailsList.Any())
            {

                foreach (var item in achievementInputDetails)
                {
                    var achievemenDetails = achievementDetailsList.Where(x => x.CultureId == item.CultureId).FirstOrDefault();
                    if (achievemenDetails != null)
                    {
                        achievemenDetails.Name = item.Name;
                        achievemenDetails.Description = item.Description;

                        _achievemenDetailstrepository.Update(achievemenDetails);
                    }
                    else
                    {
                        var newachievementDetails = new AchievementDetails
                        {
                            Name = item.Name,
                            CultureId = item.CultureId,
                            Description = item.Description,
                            AchievementId = achievementId

                        };

                        _achievemenDetailstrepository.Add(newachievementDetails);
                    }
                }
                await _achievemenDetailstrepository.SaveChangesAsync();
                await _achievementrepository.SaveChangesAsync();

                return true;
            }

            return false;
        }

        private async Task<Achievement> GetById(long id, long siteId)
        {
            return await _achievementrepository.Query()
                .Where(x => x.Id == id && x.SiteId == siteId)
                .Include(x => x.AchievementDetails)
                .FirstOrDefaultAsync();
        }


        #endregion


    }
}
