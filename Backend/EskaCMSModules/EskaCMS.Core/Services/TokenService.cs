using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Model;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Module.Core.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Core.Models.TokenVM;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using System.Globalization;

namespace EskaCMS.Core.Services
{
    public class TokenService : ITokenService
    {

        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly IRepository<UsersSites> _UserSitesRepo;
        private readonly IRepository<User> userRepository;
        private readonly IRepositoryWithTypedId<CurrentLoggedInUsers, string> currentLoggedInUsers;
        private readonly IWorkContext _workContext;
        private readonly ILoggedInUserService _loggedInUserService;

        public TokenService(IConfiguration configuration,
                            UserManager<User> userManager,
                    IRepository<UsersSites> UserSitesRepo,
                                                            IRepository<User> UserRepository,

                    IRepositoryWithTypedId<CurrentLoggedInUsers, string> CurrentLoggedInUsers,
                     ILoggedInUserService loggedInUserService,
                    IWorkContext workContext
                           )
        {

            _workContext = workContext;
            _configuration = configuration;
            _UserSitesRepo = UserSitesRepo;
            userRepository = UserRepository;
            currentLoggedInUsers = CurrentLoggedInUsers;
            _userManager = userManager;
            _loggedInUserService = loggedInUserService;

        }
        private DateTime GetConfiguredTokenExpiry(EUsersTypes userType)
        {
            try
            {
                string Duration = string.Empty;
                DateTime ConfiguredExpiry = DateTime.MinValue;
                switch (userType)
                {
                    case EUsersTypes.Admin:
                        Duration = _configuration["Authentication:Jwt:AdminAccessTokenInMinutes"];
                        ConfiguredExpiry = DateTime.Now.AddMinutes(long.Parse(Duration));
                        break;
                    case EUsersTypes.Customer:
                        Duration = _configuration["Authentication:Jwt:WebAccessTokenInMinutes"];
                        ConfiguredExpiry = DateTime.Now.AddMinutes(long.Parse(Duration));
                        break;
                    case EUsersTypes.GuestClient:
                        Duration = _configuration["Authentication:Jwt:GuestAccessTokenInYears"];
                        ConfiguredExpiry = DateTime.Now.AddYears(int.Parse(Duration));
                        break;
                    case EUsersTypes.Integration:
                        Duration = _configuration["Authentication:Jwt:IntegrationAccessTokenInHours"];
                        ConfiguredExpiry = DateTime.Now.AddHours(long.Parse(Duration));
                        break;
                    default:
                        ConfiguredExpiry = DateTime.Now.AddHours(1);
                        break;
                }
                return ConfiguredExpiry;

            }
            catch (Exception exc)
            {

                return DateTime.Now.AddHours(1);
            }
        }
        private async Task<(string Token, DateTime ExpiryDate)> GenerateAccessTokenAsync(User user)
        {

            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Authentication:Jwt:Key"]));
            DateTime ConfiguredExpiry = GetConfiguredTokenExpiry(user.Type);
            var Issuer = _configuration["Authentication:Jwt:Issuer"];
            // var TokenExpiryDate = DateTime.Now.AddMinutes(long.Parse(Duration));
            var roles = await _userManager.GetRolesAsync(user);
            var Claims = await BuildClaims(user, roles);
            JwtSecurityToken jwtToken = new JwtSecurityToken(
                    claims: Claims,
                    issuer: Issuer,
                    expires: ConfiguredExpiry,
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            string token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            return (token, ConfiguredExpiry);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private static IEnumerable<string> GetRefreshTokenCandidates(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                yield break;

            yield return refreshToken;

            if (!refreshToken.Contains('%'))
                yield break;

            var decodedRefreshToken = Uri.UnescapeDataString(refreshToken);
            if (!string.Equals(decodedRefreshToken, refreshToken, StringComparison.Ordinal))
                yield return decodedRefreshToken;
        }

        private PasswordVerificationResult VerifyRefreshToken(User user, string refreshTokenHash, string refreshToken)
        {
            foreach (var candidate in GetRefreshTokenCandidates(refreshToken))
            {
                var result = _userManager.PasswordHasher.VerifyHashedPassword(user, refreshTokenHash, candidate);
                if (result == PasswordVerificationResult.Success ||
                    result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    return result;
                }
            }

            return PasswordVerificationResult.Failed;
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token, string secret, string issuer)
        {
            try
            {
                var key = Encoding.ASCII.GetBytes(secret);


                var tokencvcvHandler = new JwtSecurityTokenHandler();
                var securityvbvbToken = tokencvcvHandler.ReadToken(token) as JwtSecurityToken;

                // var stringClaimValue = securityToken.Claims.First(claim => claim.Type == claimType).Value;

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidIssuer = issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = false,
                    RequireExpirationTime = false,
                    ClockSkew = TimeSpan.Zero
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                if (!(securityToken is JwtSecurityToken jwtSecurityToken) || !string.Equals(jwtSecurityToken.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }
                return principal;
            }
            catch (Exception exc)
            {

                throw exc;
            }

        }



        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            try
            {

                var secret = _configuration.GetSection("Authentication").GetSection("Jwt").GetSection("Key").Value;
                var issuer = _configuration.GetSection("Authentication").GetSection("Jwt").GetSection("Issuer").Value;
                return GetPrincipalFromExpiredToken(token, secret, issuer);

            }
            catch (Exception)
            {

                return null;
            }
        }

        public ClaimsPrincipal GetPrincipalFromExpiredOldToken(string oldToken)
        {
            var secret = _configuration.GetSection("Authentication").GetSection("Jwt").GetSection("OldKey").Value;
            var issuer = _configuration.GetSection("Authentication").GetSection("Jwt").GetSection("Issuer").Value;
            return GetPrincipalFromExpiredToken(oldToken, secret, issuer);
        }


        private async Task<IList<Claim>> BuildClaims(User user, IList<string> roles)
        {
            try
            {
                string UserRoles = string.Empty;
                var claims = new List<Claim>();
                bool IsParent = await userRepository.Query().AsNoTracking().CountAsync(x => x.ParentId == user.Id) > 0;

                foreach (var Role in roles)
                {
                    UserRoles += Role + ",";
                }
                claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                claims.Add(new Claim(ClaimTypes.Name, user.FullName));
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
                claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber == null ? "" : user.PhoneNumber));
                claims.Add(new Claim(ClaimTypes.Role, UserRoles));
                claims.Add(new Claim("Username", user.UserName));
                claims.Add(new Claim("UserType", user.Type.ToString()));
                claims.Add(new Claim("DefaultShippingAddressId", user.DefaultShippingAddressId == null ? "" : user.DefaultShippingAddressId.ToString()));
                claims.Add(new Claim("CurrencyCultureId", ""));
                claims.Add(new Claim("CultureId", ""));
                claims.Add(new Claim("RoleIds", user.UserRoles.Any() ? string.Join(',', user.UserRoles.Select(x => x.RoleId).ToList()) : ""));

                claims.Add(new Claim("IsParent", IsParent ? "1" : "0"));
                claims.Add(new Claim(ClaimTypes.DateOfBirth, user.DateOfBirth == null ? "" : user.DateOfBirth?.ToString()));
                await _userManager.AddClaimsAsync(user, claims);


                return claims;
            }
            catch (Exception e)
            {

                throw e;
            }
        }
        public async Task<TokenResponse> CreateToken(User user, bool includeRefreshToken)
        {
            try
            {


                return await CreateTokenWithProcess(user, includeRefreshToken);

            }
            catch (Exception exc)
            {

                throw exc;
            }

        }


        private async Task<TokenResponse> CreateTokenWithProcess(User user, bool includeRefreshToken)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var Source = _workContext.GetSource();

                var plattform = _workContext.GetCurrentPlatform();


                if (SiteId == 0)
                {
                    SiteId = await _UserSitesRepo.Query().Where(x => x.UserId == user.Id && x.IsDefault).Select(x => x.SiteId).FirstOrDefaultAsync();
                }

                TokenResponse runTimeObject = new TokenResponse();
                if (user.Status != EStatus.Active)
                    return new TokenResponse();

                var accessToken = await GenerateAccessTokenAsync(user);
                var token = accessToken.Token;
                var ExpiryDate = accessToken.ExpiryDate;

                // Check If User Can Login Or Not By CheckIfLookedOutUser 
                var IsLookedOut = await _loggedInUserService.CheckIfLookedOutUser(user.Id, SiteId);

                if (IsLookedOut)
                {
                    throw new Exception("User Is Lookedout");
                }

                runTimeObject.Token = token;
                runTimeObject.ExpiryDate = ExpiryDate;

                var RefreshTokenHash = string.Empty;

                if (includeRefreshToken)
                {
                    var refreshToken = GenerateRefreshToken();

                    RefreshTokenHash = _userManager.PasswordHasher.HashPassword(user, refreshToken);

                    runTimeObject.RefreshToken = refreshToken;

                }

                if (!IsLookedOut)
                {
                    await _loggedInUserService.AddCurrentLoggedInUsers(runTimeObject.Token, Source, RefreshTokenHash, user, SiteId);
                }
                else
                {
                    throw new Exception("USER_ACCOUNT_LOCKED");
                }


                if ((EPlatformSource)Source != EPlatformSource.Admin)
                {

                    await SetAuthCookies(runTimeObject, user);
                }


                return runTimeObject;
            }
            catch (Exception e)
            {

                throw e;
            }
        }


        public async Task<TokenResponse> RefeshToken(RefreshTokenModel model)
        {
            try
            {

                bool isOldKeyToken = false;
                TokenResponse runTimeObject = new TokenResponse();
                var principal = GetPrincipalFromExpiredToken(model.Token);
                if (principal == null)
                {

                    principal = GetPrincipalFromExpiredOldToken(model.Token);
                    if (principal == null)
                    {
                        throw new Exception("Invalid token");

                    }
                    else
                    {
                        isOldKeyToken = true;
                    }
                }


                var ClientIp = _workContext.GetIPAddress();

                var Source = _workContext.GetSource();






                var user = await _userManager.GetUserAsync(principal);

                var SiteId = _workContext.GetCurrentSiteId();

                if (SiteId == 0)
                {
                    SiteId = await _UserSitesRepo.Query().Where(x => x.UserId == user.Id && x.IsDefault).Select(x => x.SiteId).FirstOrDefaultAsync();
                }

                var IsLookedOut = await _loggedInUserService.CheckIfLookedOutUser(user.Id, SiteId);

                if (IsLookedOut)
                {
                    throw new Exception("User Is Lookedout");
                }

				var UserSession = await currentLoggedInUsers.Query()
																		  .Where(x => x.ClientIP == ClientIp && x.UserId == user.Id && x.TypeLogin == (ETypeLogin)Source)
																		  .OrderByDescending(x => x.Id)
																		  .FirstOrDefaultAsync();

				// No live session means the caller cannot prove the session is theirs. Refuse.
				if (UserSession == null)
                {
                    throw new Exception("Invalid token");
                }

                var RefreshToken = UserSession.RefreshTokenHash;

                var verifyRefreshTokenResult = VerifyRefreshToken(user, RefreshToken, model.RefreshToken);


                if (verifyRefreshTokenResult == PasswordVerificationResult.Success)
                {
                    var accessToken = await GenerateAccessTokenAsync(user);
                    var token = accessToken.Token;
                    var ExpiryDate = accessToken.ExpiryDate;

                    var refreshToken = GenerateRefreshToken();

                    var RefreshTokenHash = _userManager.PasswordHasher.HashPassword(user, refreshToken);
                    // UserSession = await currentLoggedInUsers.Query().Where(x => x.ClientIP == ClientIp && x.UserId == user.Id).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                    if (UserSession != null)
                    {
                        UserSession.RefreshTokenHash = RefreshTokenHash;
                        UserSession.ApiToken = token;
                    }
                    else
                    {

                        throw new Exception("Invalid token from user can you please login ");

                    }
                    await currentLoggedInUsers.SaveChangesAsync();


                    await _userManager.UpdateAsync(user);

                    runTimeObject.Token = token;

                    runTimeObject.RefreshToken = refreshToken;

                    runTimeObject.ExpiryDate = ExpiryDate;




                    if ((EPlatformSource)Source != EPlatformSource.Admin)
                    {

                        await SetAuthCookies(runTimeObject, user);
                    }
             
                    return runTimeObject;

                }
                else
                {
                    throw new Exception("Invalid token");
                }
            }
            catch (Exception ex)
            {


                throw ex;
            }
        }

        private async Task SetAuthCookies(TokenResponse Token, User user)
        {

            var tokenName = user.Type == EUsersTypes.GuestClient ? _configuration["Authentication:GuestTokenCookieName"].ToString() : _configuration["Authentication:UserTokenCookieName"].ToString();
            var refreshTokenName = user.Type == EUsersTypes.GuestClient ? _configuration["Authentication:GuestRefreshTokenCookieName"].ToString() : _configuration["Authentication:UserRefreshTokenCookieName"].ToString();
            await _workContext.AppendHttpOnlyCookie(tokenName, Token.Token);
            await _workContext.AppendHttpOnlyCookie(refreshTokenName, Token.RefreshToken);

        }


    }
}
