
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using System.Threading.Tasks;
using static EskaCMS.Core.Models.TokenVM;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Core.Services
{
    public interface ITokenService
    {
        Task<TokenResponse> CreateToken(User user, bool includeRefreshToken);
        Task<TokenResponse> RefeshToken(TokenVM.RefreshTokenModel model);

    }
}
