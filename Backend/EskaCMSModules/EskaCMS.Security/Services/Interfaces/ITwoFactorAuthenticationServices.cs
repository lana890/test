using EskaCMS.Security.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Services.Interfaces
{
    public interface ITwoFactorAuthenticationServices
    {
        Task<LoginResponse> VerifyTwoFactorAuthentication(VerifyTwoFactorAuthenticationViewModel model);
    }
}
