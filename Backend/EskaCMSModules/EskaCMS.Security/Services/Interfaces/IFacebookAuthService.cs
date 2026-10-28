using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Security.Models;
using System.Threading.Tasks;

namespace EskaCMS.Security.Services.Interfaces
{
    public interface IFacebookAuthService
    {
        //Task<FacebookTokenValidationResult> ValidateAccessTokenAsync(string accessToken);
        //Task<FaceBookInfoUserResult> GetUserInfoAsync(string accessToken);
        Task<LoginResponse> Login(FacebookLoginViewModel model);

    }
}
