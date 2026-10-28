using EskaCMS.Entities.Enum;
using EskaCommerce.Reviews.Models.UserInteractionModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Reviews.Services.Interfaces
{
    public interface IUserInteractionServices
    {
        Task<bool> Insert(UserInteractionViewModel model);
        Task<bool> Update(UserInteractionViewModel model);
        Task<bool> Remove(long Id);

        Task<List<UserInteractionViewModel>> Get(GeneralEnums.ERoleTypes? RoleId = null);
    }
}
