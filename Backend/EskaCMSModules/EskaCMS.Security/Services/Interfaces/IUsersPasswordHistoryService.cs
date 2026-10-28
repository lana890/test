using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Security.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Services.Interfaces
{
    public interface IUsersPasswordHistoryService
    {
        Task<bool> PasswordHistoryLog(string NewPassword, string OldPassword, ChangePasswordHistory ChangePasswordHistory, long? UserId);
    }
}