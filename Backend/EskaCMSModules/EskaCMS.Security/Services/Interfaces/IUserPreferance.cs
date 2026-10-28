using EskaCMS.Security.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Services.Interfaces
{
    public interface IUserPreferancese
    {
        Task<bool> AddNewUserPreferences(CreateUserPreferanceVM userPreferanceVM);
        Task<bool> UpdateUserPreferences(UpdateUserPreferanceVM userPreferanceVM, long userId);
        Task<GetUserPreferanceVM> GetUserPreferencesByUserId(long userIdId);
        Task<bool> RemoveIfExist(long userId, long SiteId);
    }
}
