
using EskaCMS.Security.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Services.Interfaces
{
  public  interface IRoleOptions
    {
        Task<RoleOptionsViewModel> GetByUserIdAsync(long UserId);
        Task<RoleOptionsViewModel> CreateAsync(AddRoleOptionsViewModel RoleOptions);
        Task<RoleOptionsViewModel> GetAsync(long RoleId);
        Task<Boolean> DeleteAsync(long RoleId);
    }
}
