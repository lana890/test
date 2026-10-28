using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Infrastructure;
using EskaCMS.OTP.Data;
using EskaCMS.OTP.Helpers;
using EskaCMS.OTP.Infrastructure;
using EskaCMS.OTP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.OTP.Services
{
    // Ports of EskaCMS.Core LoggedInUserServices / TokenService and EskaCMS.Security UserService.Login /
    // GuestController.CreateUser. OTP has no ASP.NET Identity (UserManager / SignInManager), so the parts
    // of Identity those flows rely on (password hashing, normalization, roles, claims) are done directly.

    public interface ILoggedInUserService
    {
        Task<bool> AddCurrentLoggedInUsers(string token, ETypeLogin Source, string RefreshToken, User user, long SiteId);
        Task<bool> CheckIfLookedOutUser(long UserId, long SiteId);
    }

    public class LoggedInUserService : ILoggedInUserService
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> SessionLocks = new();

        private readonly IRepositoryWithTypedId<CurrentLoggedInUsers, string> _CurrentLoggedInUsers;
        private readonly IRepository<SitesHostNames> _SitesHostNameRepository;
        private readonly IRepository<CustomerGroup> _CustomerGroupRepository;
        private readonly IRepository<UsersSites> _UserSitesRepository;
        private readonly OtpDbContext _context;
        private readonly IWorkContext _workContext;

        public LoggedInUserService(IRepositoryWithTypedId<CurrentLoggedInUsers, string> CurrentLoggedInUsers,
                                   IRepository<SitesHostNames> SitesHostNameRepository,
                                   IRepository<CustomerGroup> CustomerGroupRepository,
                                   IRepository<UsersSites> UsersSitesRepository,
                                   OtpDbContext context,
                                   IWorkContext workContext)
        {
            _CurrentLoggedInUsers = CurrentLoggedInUsers;
            _SitesHostNameRepository = SitesHostNameRepository;
            _CustomerGroupRepository = CustomerGroupRepository;
            _UserSitesRepository = UsersSitesRepository;
            _context = context;
            _workContext = workContext;
        }

        public async Task<bool> AddCurrentLoggedInUsers(string token, ETypeLogin Source, string RefreshToken, User user, long SiteId)
        {
            var SiteHostNames = await _SitesHostNameRepository.Query().Where(x => x.SiteId == SiteId).FirstOrDefaultAsync();

            var ClientIpAddress = _workContext.GetIPAddress();
            var sessionLock = SessionLocks.GetOrAdd($"{user.Id}:{ClientIpAddress}", _ => new SemaphoreSlim(1, 1));

            await sessionLock.WaitAsync();
            try
            {
                await CheckUserMaxSessions(user);
                await RemoveSessionIfExist(user.Id, ClientIpAddress);

                _CurrentLoggedInUsers.Add(new CurrentLoggedInUsers
                {
                    UserId = user.Id,
                    LoginDate = DateTimeOffset.Now,
                    SiteId = SiteId,
                    IsLookedOut = false,
                    HostnameId = SiteHostNames?.Id,
                    ApiToken = token,
                    RefreshTokenHash = RefreshToken,
                    ClientIP = ClientIpAddress,
                    UserType = user.Type,
                    TypeLogin = Source,
                    LoginType = LoginType.DirectLogin,
                    KillSession = false,
                });
                await _CurrentLoggedInUsers.SaveChangesAsync();
            }
            finally
            {
                sessionLock.Release();
            }
            return true;
        }

        private async Task<bool> RemoveSessionIfExist(long UserId, string Ip)
        {
            var Sessions = await _CurrentLoggedInUsers.Query()
                .Where(x => x.UserId == UserId && x.ClientIP == Ip)
                .FirstOrDefaultAsync();

            if (Sessions != null)
            {
                _CurrentLoggedInUsers.Remove(Sessions);
                try
                {
                    await _CurrentLoggedInUsers.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return true;
                }
            }
            return true;
        }

        private async Task<bool> CheckUserMaxSessions(User user)
        {
            var ClientIp = _workContext.GetIPAddress();
            var loggedInUser = await _CurrentLoggedInUsers.Query().Where(x => x.UserId == user.Id).ToListAsync();

            // CustomerGroup.Users is not mapped in OtpDbContext, so go through the join table.
            var MaxSessions = await _context.Set<CustomerGroupUser>()
                .Where(x => x.UserId == user.Id)
                .Select(x => x.CustomerGroup.MaxSessionsNumber)
                .FirstOrDefaultAsync();

            if (MaxSessions < loggedInUser.Count() + 1)
            {
                throw new Exception("You Can  Not login Now beacuse You Have Multible login In Many Devices");
            }

            var lastSession = loggedInUser.FirstOrDefault(x => x.ClientIP == ClientIp);
            if (lastSession != null && lastSession.KillSession)
            {
                throw new Exception("you are not able to login ");
            }
            return true;
        }

        public async Task<bool> CheckIfLookedOutUser(long UserId, long SiteId)
        {
            return await _UserSitesRepository.Query()
                .AsNoTracking()
                .Where(x => x.UserId == UserId && x.SiteId == SiteId)
                .Select(x => x.IsLockout)
                .FirstOrDefaultAsync();
        }
    }

    public interface ITokenService
    {
        Task<TokenResponse> CreateToken(User user, bool includeRefreshToken);
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IRepository<UsersSites> _UserSitesRepo;
        private readonly IRepository<User> userRepository;
        private readonly OtpDbContext _context;
        private readonly IWorkContext _workContext;
        private readonly ILoggedInUserService _loggedInUserService;

        public TokenService(IConfiguration configuration,
                            IPasswordHasher<User> passwordHasher,
                            IRepository<UsersSites> UserSitesRepo,
                            IRepository<User> UserRepository,
                            OtpDbContext context,
                            ILoggedInUserService loggedInUserService,
                            IWorkContext workContext)
        {
            _configuration = configuration;
            _passwordHasher = passwordHasher;
            _UserSitesRepo = UserSitesRepo;
            userRepository = UserRepository;
            _context = context;
            _loggedInUserService = loggedInUserService;
            _workContext = workContext;
        }

        private DateTime GetConfiguredTokenExpiry(EUsersTypes userType)
        {
            try
            {
                switch (userType)
                {
                    case EUsersTypes.Admin:
                        return DateTime.Now.AddMinutes(long.Parse(_configuration["Authentication:Jwt:AdminAccessTokenInMinutes"]));
                    case EUsersTypes.Customer:
                        return DateTime.Now.AddMinutes(long.Parse(_configuration["Authentication:Jwt:WebAccessTokenInMinutes"]));
                    case EUsersTypes.GuestClient:
                        return DateTime.Now.AddYears(int.Parse(_configuration["Authentication:Jwt:GuestAccessTokenInYears"]));
                    case EUsersTypes.Integration:
                        return DateTime.Now.AddHours(long.Parse(_configuration["Authentication:Jwt:IntegrationAccessTokenInHours"]));
                    default:
                        return DateTime.Now.AddHours(1);
                }
            }
            catch
            {
                return DateTime.Now.AddHours(1);
            }
        }

        private async Task<(string Token, DateTime ExpiryDate)> GenerateAccessTokenAsync(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Authentication:Jwt:Key"]));
            var ConfiguredExpiry = GetConfiguredTokenExpiry(user.Type);
            var Issuer = _configuration["Authentication:Jwt:Issuer"];

            // == UserManager.GetRolesAsync
            var roles = await _context.Set<UserRole>()
                .Where(x => x.UserId == user.Id)
                .Select(x => x.Role.Name)
                .ToListAsync();

            var Claims = await BuildClaims(user, roles);
            var jwtToken = new JwtSecurityToken(
                claims: Claims,
                issuer: Issuer,
                expires: ConfiguredExpiry,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return (new JwtSecurityTokenHandler().WriteToken(jwtToken), ConfiguredExpiry);
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private async Task<IList<Claim>> BuildClaims(User user, IList<string> roles)
        {
            var claims = new List<Claim>();
            bool IsParent = await userRepository.Query().AsNoTracking().CountAsync(x => x.ParentId == user.Id) > 0;

            string UserRoles = string.Empty;
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

            // == UserManager.AddClaimsAsync (Core_UserClaim)
            _context.Set<IdentityUserClaim<long>>().AddRange(claims.Select(c => new IdentityUserClaim<long>
            {
                UserId = user.Id,
                ClaimType = c.Type,
                ClaimValue = c.Value
            }));
            await _context.SaveChangesAsync();

            return claims;
        }

        public async Task<TokenResponse> CreateToken(User user, bool includeRefreshToken)
        {
            var SiteId = _workContext.GetCurrentSiteId();
            var Source = _workContext.GetSource();

            if (SiteId == 0)
            {
                SiteId = await _UserSitesRepo.Query().Where(x => x.UserId == user.Id && x.IsDefault).Select(x => x.SiteId).FirstOrDefaultAsync();
            }

            var runTimeObject = new TokenResponse();
            if (user.Status != EStatus.Active)
                return new TokenResponse();

            var accessToken = await GenerateAccessTokenAsync(user);

            // Check If User Can Login Or Not By CheckIfLookedOutUser
            var IsLookedOut = await _loggedInUserService.CheckIfLookedOutUser(user.Id, SiteId);
            if (IsLookedOut)
            {
                throw new Exception("User Is Lookedout");
            }

            runTimeObject.Token = accessToken.Token;
            runTimeObject.ExpiryDate = accessToken.ExpiryDate;

            var RefreshTokenHash = string.Empty;
            if (includeRefreshToken)
            {
                var refreshToken = GenerateRefreshToken();
                RefreshTokenHash = _passwordHasher.HashPassword(user, refreshToken);
                runTimeObject.RefreshToken = refreshToken;
            }

            await _loggedInUserService.AddCurrentLoggedInUsers(runTimeObject.Token, Source, RefreshTokenHash, user, SiteId);

            if ((EPlatformSource)Source != EPlatformSource.Admin)
            {
                await SetAuthCookies(runTimeObject, user);
            }

            return runTimeObject;
        }

        private async Task SetAuthCookies(TokenResponse Token, User user)
        {
            var tokenName = user.Type == EUsersTypes.GuestClient ? _configuration["Authentication:GuestTokenCookieName"] : _configuration["Authentication:UserTokenCookieName"];
            var refreshTokenName = user.Type == EUsersTypes.GuestClient ? _configuration["Authentication:GuestRefreshTokenCookieName"] : _configuration["Authentication:UserRefreshTokenCookieName"];
            await _workContext.AppendHttpOnlyCookie(tokenName, Token.Token);
            await _workContext.AppendHttpOnlyCookie(refreshTokenName, Token.RefreshToken);
        }
    }

    public interface IUserService
    {
        Task<LoginResponse> Login(LoginViewModel model);
        Task<User> CreateGuestUser(long SiteId, string ClientIP);
    }

    public class UserService : IUserService
    {
        private readonly IRepository<User> _userrepository;
        private readonly IRepository<Site> _siteRepository;
        private readonly IRepository<UserLoginTransactions> _userLoginTransactions;
        private readonly OtpDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IWorkContext _workContext;

        private readonly string AdBaseUrl;
        private readonly string adpassword;
        private readonly string adusername;

        public UserService(IRepository<User> userrepository,
                           IRepository<Site> siteRepository,
                           IRepository<UserLoginTransactions> userLoginTransactions,
                           OtpDbContext context,
                           IPasswordHasher<User> passwordHasher,
                           ITokenService tokenService,
                           IWorkContext workContext,
                           IConfiguration configuration)
        {
            _userrepository = userrepository;
            _siteRepository = siteRepository;
            _userLoginTransactions = userLoginTransactions;
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _workContext = workContext;

            AdBaseUrl = configuration.GetSection("ActiveDirectory:ActiveDirectoryUrl")?.Value;
            adpassword = configuration.GetSection("ActiveDirectory:AdPassword")?.Value;
            adusername = configuration.GetSection("ActiveDirectory:AdUserName")?.Value;
        }

        private IQueryable<User> UsersWithLoginData()
        {
            return _userrepository.Query()
                .Include(x => x.UserSites)
                .Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup)
                .Include(x => x.UserRoles).ThenInclude(x => x.Role)
                .Include(x => x.UserOptions);
        }

        public async Task<LoginResponse> Login(LoginViewModel model)
        {
            try
            {
                long SiteId = _workContext.GetCurrentSiteId();
                if (SiteId == 0)
                    throw new Exception("SiteId cannot be null");

                string ModelUsername = model.Username.ToUpper();
                var users = UsersWithLoginData().Where(x => x.UserSites.Any(a => a.SiteId == SiteId));

                User user;
                switch (model.LoginType)
                {
                    case LoginTypes.UserName:
                        user = await users.Where(x => x.NormalizedUserName == ModelUsername).FirstOrDefaultAsync();
                        break;
                    case LoginTypes.Email:
                        user = await users.Where(x => x.NormalizedEmail == ModelUsername).FirstOrDefaultAsync();
                        break;
                    case LoginTypes.PhoneNumber:
                        user = await users.Where(x => x.PhoneNumber == ModelUsername).FirstOrDefaultAsync();
                        break;
                    case LoginTypes.AdditionInfo:
                        user = await users.Where(x => x.UserOptions.Any(o => o.NormalizedPropertyName == model.AdditionalInfoValue.ToUpper() && o.NormalizedValue == ModelUsername)).FirstOrDefaultAsync();
                        break;
                    case LoginTypes.UsernameOrEmail:
                        user = await users.Where(x => x.NormalizedUserName == ModelUsername || x.NormalizedEmail == ModelUsername).FirstOrDefaultAsync();
                        break;
                    case LoginTypes.UsernameOrPhone:
                        user = await users.Where(x => x.NormalizedUserName == ModelUsername || x.PhoneNumber == ModelUsername).FirstOrDefaultAsync();
                        break;
                    case LoginTypes.PhoneOrEmail:
                        user = await users.Where(x => x.NormalizedEmail == ModelUsername || x.PhoneNumber == ModelUsername).FirstOrDefaultAsync();
                        break;
                    default:
                        user = await users.Where(x => x.NormalizedUserName.ToUpper() == ModelUsername).FirstOrDefaultAsync();
                        break;
                }

                if (user == null)
                {
                    throw new Exception("INVALID_CREDENTIALS");
                }

                if (model.FromGoogleOrFacebookOrApple)
                {
                    if (user.IsFacebookRegistered || user.IsGoogleRegistered || user.IsAppleRegistered)
                    {
                        return await DCMSLoginWithoutPassword(model, SiteId);
                    }
                }

                return await DCMSLogin(user, model, SiteId);
            }
            catch (Exception e)
            {
                e = ExceptionHelper.ThrowDeepException(e);
                throw new Exception(e.Message);
            }
        }

        private async Task<LoginResponse> DCMSLogin(User user, LoginViewModel model, long SiteId)
        {
            try
            {
                bool isPasswordValid;
                if (user == null || user.UserRoles == null || user.UserSites == null)
                    throw new Exception("INVALID_CREDENTIALS");

                if (user.Status != EStatus.Active)
                    throw new Exception("INACTIVE_USER");

                if (model.AuthorizedGroups != null && model.AuthorizedGroups.Count > 0)
                {
                    var authorizedUserGroups = user.CustomerGroups.Where(x => model.AuthorizedGroups.Any(a => a == x.CustomerGroupId)).ToList();
                    if (!authorizedUserGroups.Any())
                    {
                        throw new Exception("INVALID_CREDENTIALS");
                    }
                }

                await CheckExpiryPasswordForUser(user);

                if (!model.ValidateFromAd)
                {
                    isPasswordValid = await CheckPasswordSignIn(user, model.Password);
                }
                else
                {
                    isPasswordValid = await ValidateAdIntegration(model.Password, model.Username);
                }

                if (isPasswordValid)
                {
                    await TwoFactorEnabled(user, 0);
                    // NOTE: the main system also publishes UserSignedIn here (MediatR) so the ShoppingCart /
                    // ProductComparison modules migrate the guest's cart; those modules do not run in OTP.
                    await AddLogUserLogin(user.Id, UserLoggingType.LogginSuccess);

                    return new LoginResponse
                    {
                        Culture = GlobalConfiguration.DefaultCulture,
                        UserAdditionData = user.UserOptions.Select(x => new UserOptionsVM
                        {
                            PropertyName = x.PropertyName,
                            Value = x.Value,
                            ControlType = x.ControlType,
                            IsEditable = x.IsEditable,
                            IsVisible = x.IsVisible,
                            Id = x.Id
                        }).ToList(),
                        Email = user.Email,
                        ImageUrl = user.Image,
                        Role = user.UserRoles.Select(x => new UserRoleVM { RoleId = x.Role.Id, RoleName = x.Role.Name }).ToList(),
                        Token = await _tokenService.CreateToken(user, true)
                    };
                }

                throw new Exception("INVALID_CREDENTIALS");
            }
            catch (Exception exc)
            {
                throw new Exception(exc.Message);
            }
        }

        private async Task<LoginResponse> DCMSLoginWithoutPassword(LoginViewModel model, long SiteId)
        {
            try
            {
                User user = await UsersWithLoginData()
                    .Where(x => x.NormalizedUserName == model.Username.ToUpper() && x.UserSites.Any(a => a.SiteId == SiteId))
                    .FirstOrDefaultAsync();

                if (user == null || user.UserRoles == null || user.UserSites == null)
                    throw new Exception("Something went wrong");

                if (user.Status == EStatus.Inactive || user.Status == EStatus.Deleted)
                    throw new Exception("Something went wrong");

                var runTimeObject = new LoginResponse
                {
                    Token = await _tokenService.CreateToken(user, true),
                    Role = user.UserRoles.Select(x => new UserRoleVM { RoleId = x.Role.Id, RoleName = x.Role.Name }).ToList(),
                    Email = user.Email,
                    ImageUrl = user.Image
                };
                await AddLogUserLogin(user.Id, UserLoggingType.LogginSuccess);
                return runTimeObject;
            }
            catch (Exception exc)
            {
                throw new Exception(exc.Message);
            }
        }

        // == SignInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false)
        private async Task<bool> CheckPasswordSignIn(User user, string password)
        {
            if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
                return false;

            if (string.IsNullOrEmpty(user.PasswordHash) || password == null)
                return false;

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
                return false;

            var changed = false;
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                changed = true;
            }
            if (user.AccessFailedCount != 0)
            {
                user.AccessFailedCount = 0;
                changed = true;
            }
            if (changed)
            {
                user.ConcurrencyStamp = Guid.NewGuid().ToString();
                await _userrepository.SaveChangesAsync();
            }
            return true;
        }

        private async Task<bool> CheckExpiryPasswordForUser(User user)
        {
            var Today = DateTime.Now;
            var Days = user.CustomerGroups.Select(x => x.CustomerGroup.PasswordExpiryInDays).FirstOrDefault();

            if (user.lastPasswordUpdate == null)
            {
                user.lastPasswordUpdate = Today;
                await _userrepository.SaveChangesAsync();
            }
            else if (Days != 0)
            {
                var UserLastChangePassword = user.lastPasswordUpdate.Value.AddDays(Days);
                if (DateTime.Compare(UserLastChangePassword, Today) <= 0)
                {
                    throw new Exception("Your password has expired.Kindly update your password to proceed");
                }
            }
            return true;
        }

        private async Task TwoFactorEnabled(User user, long SiteId = 0)
        {
            if (SiteId == 0)
            {
                SiteId = _workContext.GetCurrentSiteId();
            }
            var Site = await _siteRepository.Query().Where(x => x.Id == SiteId).FirstOrDefaultAsync();
            if (Site.EnableTwoFactorAuthentication
                && user.CustomerGroups.Any(x => x.CustomerGroup.EnableTwoFactorAuthentication)
                && user.TwoFactorEnabled)
            {
                throw new Exception("Two factor auth is required");
            }
        }

        private async Task<bool> ValidateAdIntegration(string password, string username)
        {
            var credentialsCache = new NetworkCredential(adusername, adpassword);
            var handler = new HttpClientHandler { Credentials = credentialsCache };
            var payload = new loginRequestAdViewModel { Password = password, Username = username };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(AdBaseUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                HttpResponseMessage response = await client.PostAsync("adapi/Identity/ValidateAd", content);
                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var Result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return Convert.ToBoolean(Result.content);
                }
                return false;
            }
        }

        // == UserLoginTransactionsService.AddLogUserLogin
        private async Task<bool> AddLogUserLogin(long UserId, UserLoggingType loggingType, long? SiteId = null)
        {
            try
            {
                _userLoginTransactions.Add(new UserLoginTransactions
                {
                    UserSiteId = await _workContext.GetCurrentUserSiteId(UserId, SiteId),
                    HostName = _workContext.GetCurrentHostname(),
                    LoginDate = DateTimeOffset.Now,
                    UserIP = _workContext.GetIPAddress(),
                    LoggingType = loggingType,
                });
                await _userLoginTransactions.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // == GuestController.CreateDumyUser (UserManager.CreateAsync + AddToRoleAsync)
        public async Task<User> CreateGuestUser(long SiteId, string ClientIP)
        {
            var dummyEmail = string.Format("{0}@guest.EskaCMS.com", Guid.NewGuid());
            var newUser = new User
            {
                FullName = "Guest",
                Email = dummyEmail,
                UserName = dummyEmail,
                Type = EUsersTypes.GuestClient,
                CreationDate = DateTimeOffset.Now,
                CreatedById = 1,
                ClientIP = ClientIP,
                Status = EStatus.Active,
                StatusDate = DateTimeOffset.Now,
            };
            newUser.NormalizedEmail = dummyEmail.Normalize().ToUpperInvariant();
            newUser.NormalizedUserName = dummyEmail.Normalize().ToUpperInvariant();
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, "DCMs@123");
            newUser.SecurityStamp = Guid.NewGuid().ToString("N").ToUpperInvariant();

            newUser.UserSites.Add(new UsersSites { User = newUser, SiteId = SiteId, IsDefault = true, CultureId = null, CountryId = null });
            newUser.CustomerGroups.Add(new CustomerGroupUser
            {
                CustomerGroupId = 2, // guest
                User = newUser
            });

            var guestRoleName = ERoleTypes.Guest.ToString().ToUpperInvariant();
            var guestRole = await _context.Set<Role>().FirstOrDefaultAsync(r => r.NormalizedName == guestRoleName);
            if (guestRole == null)
                throw new InvalidOperationException($"Role {guestRoleName} does not exist.");

            newUser.UserRoles.Add(new UserRole { User = newUser, RoleId = guestRole.Id, Role = guestRole });

            _userrepository.Add(newUser);
            await _userrepository.SaveChangesAsync();

            return newUser;
        }
    }
}
