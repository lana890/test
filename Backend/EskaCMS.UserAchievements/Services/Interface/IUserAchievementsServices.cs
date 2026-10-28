using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.UserAchievements.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.UserAchievements.Services.Interface
{
    public interface IUserAchievementsServices
    {
         Task<bool> ChangeAchievementStatus(long achievementId, EStatus status);
        Task<bool> CreateAchievement(CreateAchievementVM model);
        Task<bool> UpdateAchievement(AchievementUpdateVM model);
        Task<PaginationResult<AchievementListVM>> GetAllAchivements(SmartTableParam model);
        Task<PaginationResult<UserAchievementVM>> GetUserAchivements(SmartTableParam model);


    }
}
