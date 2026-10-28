using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Security.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Services.Interfaces
{
    public interface IGoogleAuthService
    {
        Task<LoginResponse> Login(GoogleLoginViewModel model);

    }
}
