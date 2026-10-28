using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.BusinessModels.Manage;
using EskaCMS.Core.Cache;
using EskaCMS.Core.Events;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Manage;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Filters.Helpers.Interfaces;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services;
using EskaCMS.Security.Services.Interfaces;
using EskaCMS.Sites.Services.Interfaces;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using FirebaseAdmin.Messaging;
using HtmlAgilityPack;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Web.Administration;
using MimeDetective.Storage;
using Newtonsoft.Json;
using OfficeOpenXml;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Controllers
{

    [Route("api/admin")]
    [ApiController]
    // [Authorize]
    public class AdminApiController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        // private readonly RoleManager<Role> _roleManager;
        private readonly IWorkContext _workcontrextservice;
        private readonly IRepository<CustomerGroup> _customerGrouprepository;
        private readonly IRepository<UsersSites> _UsersSitesRepository;
        private readonly IMediator _mediator;
        private readonly IRepository<UsersPasswordHistory> usersPasswordHistoryRepo;
        // private readonly ISites _SitesService;
        // private readonly IdcmsLogService idcmsLogService;
        private readonly IUserService _UserService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IRepository<User> _userrepository;
        // private readonly IEmailSender _emailSender;
        private readonly ITokenService _tokenService;
        private readonly IAdminApi _AdminApi;
        private readonly IHashingHelper hashingHelper;
        // private readonly IUsersPasswordHistoryService _usersPasswordHistory;

        private readonly ITestNotifier _testNotifier;
        // private readonly ILoggedInUserService _loggedInUserService;
        // private readonly IRepository<SitesHostNames> _SitesHostNameRepository;
        private readonly IWorkContext _workContext;
        private readonly IUserLoginTransactionsService _userLoginTransactions;
        private readonly IUserWorkingHoursService _userWorkingHours;
        // private readonly IRepository<PageTemplates> _pageTemplateRepo;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;


        // Confirmed unused: never assigned and never read - the method that uses the name
        // declares its own local `chromePath` (compiler warning CS0169).
        // private string chromePath;
        public AdminApiController(UserManager<User> userManager,
                                  SignInManager<User> signInManager,
                                  IWorkContext workcontrextservice,
                                  IRepository<CustomerGroup> customerGrouprepository,
                                  /* ISites SitesService, */
                                  /* IdcmsLogService IdcmsLogService, */
                                   /* IRepository<SitesHostNames> SitesHostName, */
                                  /* RoleManager<Role> roleManager, */
                                  IUserService UserService,
                                  IRepository<UsersSites> UsersSitesRepository,
                                  /* IRepository<PageTemplates> pageTemplateRepo, */
                                  IMediator mediator,
                                              IRepository<UsersPasswordHistory> UsersPasswordHistoryRepo,

                                  IEmailTemplateService emailTemplateService,
                                  IRepository<User> userrepository,
                                  /* IEmailSender emailSender, */
                                   IAdminApi AdminApi,
                                     IHashingHelper HashingHelper,
                                   /* IUsersPasswordHistoryService usersPasswordHistory, */

                                   IUserWorkingHoursService userWorkingHours,
                                   ITestNotifier testNotifier,
                                   /* ILoggedInUserService loggedInUserService, */
                                   /* IRepository<SitesHostNames> SitesHostNameRepository, */
                                               IWorkContext workContext,
                                               IUserLoginTransactionsService userLoginTransactions,
                                               IConfiguration configuration,
                                               ICacheService cacheService,



        ITokenService tokenService)
        {

            _userManager = userManager;
            _signInManager = signInManager;
            _UserService = UserService;
            // _roleManager = roleManager;
            _workcontrextservice = workcontrextservice;
            _customerGrouprepository = customerGrouprepository;
            // _emailSender = emailSender;
            _UsersSitesRepository = UsersSitesRepository;
            _mediator = mediator;
            usersPasswordHistoryRepo = UsersPasswordHistoryRepo;
            _emailTemplateService = emailTemplateService;
            _userrepository = userrepository;
            // _SitesService = SitesService;
            // idcmsLogService = IdcmsLogService;
            _tokenService = tokenService;
            _AdminApi = AdminApi;
            hashingHelper = HashingHelper;
            // _usersPasswordHistory = usersPasswordHistory;
            _testNotifier = testNotifier;
            // _loggedInUserService = loggedInUserService;
            // _SitesHostNameRepository = SitesHostNameRepository;
            _workContext = workContext;
            _userLoginTransactions = userLoginTransactions;
            _userWorkingHours = userWorkingHours;
            // _pageTemplateRepo = pageTemplateRepo;
            _configuration = configuration;
            _cacheService = cacheService;
        }


        [HttpPost]
        [Route("UpdateDeviceToken")]
        [Authorize]
        public async Task<IActionResult> UpdateDeviceToken([FromBody] UpdateDeviceToken deviceToken )
        {
            try
            {
                if (!string.IsNullOrEmpty(deviceToken.DeviceToken))
                {
                    List<string> lst = new List<string>();
                    lst.Add(deviceToken.DeviceToken);
                    var user = await _workcontrextservice.GetCurrentUser();
                    if (user != null)
                    {
                        user.DeviceToken = deviceToken.DeviceToken;
                        await _userManager.UpdateAsync(user);

                        var usersGroups = _customerGrouprepository.Query().Where(x => x.Users.Any(x => x.UserId == user.Id) &&  !x.IsDeleted && !string.IsNullOrEmpty(x.FireBaseTopic)).ToList();

                        foreach (var groupTopic in usersGroups)
                        {

                            var response = await FirebaseMessaging.DefaultInstance.SubscribeToTopicAsync(lst, groupTopic.FireBaseTopic);

                        }
                        return Ok("Device token has been updated");
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, "User not found");
                    }

                }

                return BadRequest("failed to update device token");



            }
            catch (Exception exc)
            {

                return StatusCode(StatusCodes.Status500InternalServerError, exc.Message);
            }
        }




        //[HttpPost]
        //[Route("UpdateDeviceToken")]



        [HttpPost]
        [Route("GetUserLoginTransactions")]
        public async Task<IActionResult> GetUserLoginTransactions(PaginationUserLoginTransaction Model)
        {
            try
            {
                return Ok(await _userLoginTransactions.GetUserLogins(Model));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("IsMobileExist/{mobile}")]
        [AllowAnonymous]
        [EnableCors("ApiCorsPolicy")]
        public async Task<bool> IsMobileExist(string mobile)
        {
            try
            {
                var user = await _userManager.Users.Where(x => x.PhoneNumber.Contains(mobile.ToString())).Select(x => x.DeviceToken).FirstOrDefaultAsync();
                if (!string.IsNullOrEmpty(user))
                {

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        //  [ValidateAntiForgeryToken] 
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] AdminApiViewModel model)
        {
            try
            {
                const short RoleOptionValue = 1;
                string username = model.Username.Trim().ToUpper();
                var siteId = _workContext.GetCurrentSiteId();

                var userSite = await _UsersSitesRepository.Query()
                    .Where(x => x.User.NormalizedUserName == username
                             && x.User.Type != EUsersTypes.GuestClient
                             && x.User.UserRoles.Any(r => r.Role.Type == ERoleTypes.Admin))
                    .Include(x => x.User)
                    .Include(x => x.User.UserRoles)
                        .ThenInclude(x => x.Role)
                            .ThenInclude(x => x.RoleOptions.Where(ro => ro.OptionValue == RoleOptionValue))
                                .ThenInclude(x => x.Option)
                    .Select(x => new { x.User, x.IsDefault, x.SiteId })
                    .ToListAsync();

                if (userSite.Count == 0)
                    return BadRequest("Invalid Credentials");

                var userInfo = userSite.FirstOrDefault(x => x.IsDefault) ?? userSite.First();

                var result = await _signInManager.CheckPasswordSignInAsync(
                    userInfo.User, model.Password, lockoutOnFailure: true);

                if (result.RequiresTwoFactor)
                    return Ok("2FA");

                if (result.IsLockedOut)
                {
                    await _userLoginTransactions.AddLogUserLogin(
                        userInfo.User.Id, UserLoggingType.loginLocked, userInfo.SiteId);
                    return BadRequest("User account locked out., wait for 5 Minutes");
                }

                if (!result.Succeeded)
                    return BadRequest("User is not authorized");

                await _UserService.CheckExpiryPasswordForUser(userInfo.User);
                await _UserService.TwoFactorEnabled(userInfo.User, userInfo.SiteId);

                var isInWorkingHours = await _userWorkingHours.IsInWorkingHours(
                    userInfo.User.Id, userInfo.SiteId);

                if (!isInWorkingHours)
                    return BadRequest("You can't login right now, please try again later");

                var optionsCodes = userInfo.User.UserRoles
                    .SelectMany(r => r.Role.RoleOptions)
                    .Select(ro => ro.Option.Code)
                    .Distinct()
                    .ToList();

                var response = new AdminLoginResponseVM
                {
                    SiteId = userInfo.SiteId,
                    IsDefault = userInfo.IsDefault,
                    Token = await _tokenService.CreateToken(userInfo.User, true),
                    OptionsCodes = optionsCodes
                };

                await _mediator.Publish(new UserSignedIn
                {
                    UserId = userInfo.User.Id,
                    SiteId = userInfo.SiteId
                });
                
                await _userLoginTransactions.AddLogUserLogin(
                    userInfo.User.Id, UserLoggingType.LogginSuccess, userInfo.SiteId);

                return Ok(response);
            }
            catch (Exception exc)
            {
                if (exc.Message.Contains("Two factor auth is required"))
                {
                    return new ObjectResult(new
                    {
                        message = "Two factor auth is required",
                        IsTwoFactorEnabled = true
                    })
                    { StatusCode = StatusCodes.Status207MultiStatus };
                }
               return BadRequest(exc.Message);
            }
        }
        private string DecodeFrom64(string encodedData)
        {
            System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
            System.Text.Decoder utf8Decode = encoder.GetDecoder();
            byte[] todecode_byte = Convert.FromBase64String(encodedData);
            int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
            char[] decoded_char = new char[charCount];
            utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
            string result = new String(decoded_char);
            return result;
        }
        [HttpPost]
        [AllowAnonymous]
        [Route("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] Core.Entities.ForgotPasswordViewModel model)
        {
            try
            {


                if (ModelState.IsValid)
                {
                    User user = new User();

                    user = await _userrepository.Query()
                                                .Include(x => x.UserRoles)
                                                .Where(x => x.UserName.ToLower() == model.Email.ToLower()
                                                    && x.UserRoles.Any(x => x.Role.Type == ERoleTypes.Admin))
                                                .FirstOrDefaultAsync();

                    if (user == null)
                    {
                        // Don't reveal that the user does not exist or is not confirmed
                        return BadRequest("No email found");
                    }

                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                    // Send an email with this link
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(code);
                    string convertedCode = Base64UrlEncode(toEncodeAsBytes);
                    //var callbackUrl = Url.Action(Acco "ResetPassword", "AccountController", new { user = user.Id, code = code }, Request.Scheme);


                    var callbackUrl = $"{model.Host}/reset-password/{user.Id}/{convertedCode}";
                    var lstemail = new List<string>();
                    lstemail.Add(model.Email);

                    NotificationObject data = new NotificationObject();

                    string EmailBody;
                    EmailTemplateVM emailTemplateObj = null;
                    if (model.EmailTemplateId != null)
                    {
                        emailTemplateObj = await _emailTemplateService.GetEmailTemplateById(model.EmailTemplateId);

                    }
                    if (emailTemplateObj != null)
                    {


                        List<EmailParamsVM> Params = new();
                        Params.Add(new EmailParamsVM { Param = "callbackurl", Value = callbackUrl });
                        Params.Add(new EmailParamsVM { Param = "username", Value = user.FullName });

                        data.EmailOptions.Params = Params;
                        data.EmailOptions.TemplateId = model.EmailTemplateId;
                        EmailBody = emailTemplateObj.EmailBody;


                    }
                    else
                    {

                        EmailBody = $"<html><body><span>Please reset your password by clicking here : </span> <a href='{callbackUrl}'>link</a></body></html>";


                    }

                    List<long> Ids = new List<long>();
                    Ids.Add(user.Id);
                    data.UserId = Ids.ToArray();
                    data.MessageTitle = "Reset Password";
                    data.MessageBody = EmailBody;
                    data.SendValue = lstemail;
                    data.Severity = 0;
                    //data.EmailOptions = data.EmailSenderVM;
                    await _testNotifier.SendMessageAsync(NotificationType.Email, null, data, 1);

                    // await _emailSender.SendEmailAsync(lstemail, "Reset Password", EmailBody, 0, true);
                    return Ok();

                }

                // If we got this far, something failed, redisplay form
                return BadRequest("ForgotPasswordViewModel not valid");
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("reset-password")]

        public async Task<IActionResult> ResetPassword([FromBody] Core.Entities.ResetPasswordViewModel model)
        {
            try
            {
                long? UserSiteId = null;


                var user = await _userrepository.Query()
                                            .Where(x => (x.Id == model.UserId))
                                            .Include(x => x.UserSites)
                                            .FirstOrDefaultAsync();

                if (user.UserSites.Count() > 0)
                {
                    UserSiteId = user.UserSites.Where(x => x.IsDefault).Select(x => x.Id).FirstOrDefault();
                }
                if (UserSiteId == null)
                {
                    UserSiteId = user.UserSites.Select(x => x.Id).FirstOrDefault();

                    if (UserSiteId == null)
                    {

                        return BadRequest(" Admin User Not Assigned To Any Site");

                    }
                }

                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return BadRequest("User Not Found");
                }
                string code;

                byte[] encodedDataAsBytes = Base64UrlDecode(model.Code);
                code = System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);


                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var ResetResult = await _userManager.ResetPasswordAsync(user, code, model.Password);

                //  await _userrepository.SaveChangesAsync();

                if (ResetResult.Succeeded)
                {

                    var oldPassword = await usersPasswordHistoryRepo.Query()
                    .Include(u => u.UsersSites)
                    .Where(x => x.UsersSites.UserId == user.Id)
                    .OrderByDescending(x => x.Date)
                    .FirstOrDefaultAsync();

                    var passwordHistoryChanges = new UsersPasswordHistory()
                    {
                        NewPassword = hashingHelper.GetHash(model.Password),
                        OldPassword = oldPassword == null ? null : hashingHelper.GetHash(oldPassword.NewPassword),
                        UsersSitesId = UserSiteId.Value,
                        Date = DateTime.Now,
                        ChangePasswordHistory = ChangePasswordHistory.forgetPassword
                    };

                    usersPasswordHistoryRepo.Add(passwordHistoryChanges);
                    await usersPasswordHistoryRepo.SaveChangesAsync();
                    await _UserService.LastUpdatePasswordEdit(user.Id);
                    return Ok();
                }
                else
                {

                    return BadRequest(ResetResult.Errors.Select(x => x.Description).FirstOrDefault());

                }
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }



        }
        private static string Base64UrlEncode(byte[] input)
        {
            var output = Convert.ToBase64String(input)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", string.Empty);
            return output;
        }




        [HttpGet]
        [Route("GetPasswordHistory/{UserId}/{ChangePasswordType?}")]
        public async Task<IActionResult> GetPasswordHistory([FromRoute] long UserId, [FromRoute] ChangePasswordHistory? ChangePasswordType = null)
        {
            try
            {
                return Ok(await _UserService.GetPasswordHistory(UserId, ChangePasswordType));

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [HttpGet("GetSiteMap")]
        [Produces("application/xml")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSiteMap()
        {
            try
            {
                string hostName = Request.Query["url"].ToString();
                string scheme = HttpContext.Request.Headers["X-Forwarded-Proto"].ToString();
                if (string.IsNullOrWhiteSpace(scheme))
                    scheme = "https";

                if (string.IsNullOrWhiteSpace(hostName))
                    return BadRequest("Invalid or missing host name");

                var siteDetails = await _AdminApi.GetSiteDetailByHostName(hostName);
                if (siteDetails == null)
                    return BadRequest("No sites for this hostname");

                // ✅ Sequential calls (no Task.WhenAll)
                var relatedPages = await _AdminApi.GetRelatedPages();
                var relatedPosts = await _AdminApi.GetRelatedPosts();
                var relatedCategories = await _AdminApi.GetRelatedCategory();
                var relatedProducts = await _AdminApi.GetRelatedProducts();
                var relatedBrands = await _AdminApi.GetRelatedBrands();

                var hosts = siteDetails.HostNames;
                if (hosts == null || !hosts.Any())
                    return BadRequest("No host mappings found");

                XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
                XNamespace xhtml = "http://www.w3.org/1999/xhtml";

                // ✅ Build sitemap efficiently
                var urlSet = new XElement(ns + "urlset", new XAttribute(XNamespace.Xmlns + "xhtml", xhtml));

                void AddSection<T>(IEnumerable<T> items, IEnumerable<dynamic> related, string slugProperty)
                {
                    foreach (var url in BuildSitemapSection(items, related, hosts, scheme, hostName, ns, xhtml, slugProperty))
                        urlSet.Add(url);
                }

                AddSection(siteDetails.Pages, relatedPages, "PageSlug");
                AddSection(siteDetails.Posts, relatedPosts, "PostSlug");
                AddSection(siteDetails.Categories, relatedCategories, "Slug");
                AddSection(siteDetails.Products, relatedProducts, "Slug");
                AddSection(siteDetails.Brands, relatedBrands, "Slug");

                // ✅ Create the XML document
                var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), urlSet);

                // ✅ Force Content-Type to XML
                Response.ContentType = "application/xml; charset=utf-8";
                var xml = doc.ToString(SaveOptions.DisableFormatting);

                return File(
                    Encoding.UTF8.GetBytes(xml),
                    "application/xml; charset=utf-8"
                );

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private static IEnumerable<XElement> BuildSitemapSection<T>(
       IEnumerable<T> items,
       IEnumerable<dynamic> related,
       IEnumerable<dynamic> hosts,
       string scheme,
       string mainHost,
       XNamespace ns,
       XNamespace xhtml,
       string slugProperty)
        {
            if (items == null)
                yield break;

            // ✅ Cache property accessors to avoid reflection cost on each loop
            var type = typeof(T);
            var slugProp = type.GetProperty(slugProperty);
            var relativeUrlProp = type.GetProperty("RelativeUrl");
            var cultureIdProp = type.GetProperty("CultureId");
            var modDateProp = type.GetProperty("ModificationDate");
            var parentIdProp = type.GetProperty("ParentId");

            foreach (var item in items)
            {
                // --- Slug ---
                var slug = slugProp?.GetValue(item)?.ToString();
                if (string.IsNullOrWhiteSpace(slug))
                    continue;

                var relativeUrl = relativeUrlProp?.GetValue(item)?.ToString();
                var itemUrl = string.IsNullOrWhiteSpace(relativeUrl) ? slug : relativeUrl;

                // --- Culture ---
                var cultureId = Convert.ToInt64(cultureIdProp?.GetValue(item) ?? 0);

                // --- Modification Date (supports DateTimeOffset and DateTime) ---
                var modificationValue = modDateProp?.GetValue(item);
                DateTime modificationDate = modificationValue switch
                {
                    DateTimeOffset dto => dto.UtcDateTime,
                    DateTime dt => dt,
                    _ => DateTime.UtcNow
                };

                // --- Parent ID ---
                var parentId = parentIdProp?.GetValue(item);

                // --- Host Mapping ---
                var host = hosts.FirstOrDefault(h => h.CultureId == cultureId);
                if (host == null)
                    continue;

                // --- Build URL Node ---
                var url = new XElement(ns + "url",
                    new XElement(ns + "loc", $"{scheme}://{mainHost}/{itemUrl}"),
                    new XElement(ns + "lastmod", modificationDate.ToString("yyyy-MM-dd")),
                    new XElement(ns + "changefreq", "monthly"),
                    new XElement(ns + "priority", "0.8")
                );

                // --- Alternate Links ---
                if (related != null)
                {
                    var alts = related
                        .Where(r => Equals(r.ParentId, parentId))
                        .OrderBy(r => r.CultureId);

                    foreach (var alt in alts)
                    {
                        var altHost = hosts.FirstOrDefault(h => h.CultureId == alt.CultureId);
                        if (altHost != null && !string.IsNullOrEmpty(alt.CultureCode))
                        {
                            var altRelativeUrl = GetStringPropertyValue(alt, "RelativeUrl");
                            var altSlug = GetStringPropertyValue(alt, "Slug");
                            var altUrl = string.IsNullOrWhiteSpace(altRelativeUrl) ? altSlug : altRelativeUrl;

                            url.Add(new XElement(xhtml + "link",
                                new XAttribute("rel", "alternate"),
                                new XAttribute("hreflang", alt.CultureCode),
                                new XAttribute("href", $"{scheme}://{altHost.HostNames}/{altUrl}")
                            ));
                        }
                    }
                }

                yield return url;
            }
        }

        private static string GetStringPropertyValue(object item, string propertyName)
        {
            return item?.GetType().GetProperty(propertyName)?.GetValue(item)?.ToString();
        }



        // NOTE!!
        // In Order to Make this api work you need 2 things
        // first the below rewrite rule
        // seconde to activate cashing from appsettings.json
        // NOTE!!
        // In Order to Make this api work you need 2 things
        // first the below rewrite rule
        // seconde to activate cashing from appsettings.json
        /* 
         <rule name="Redirect Crawler SSR" stopProcessing="true"> 
             <match url="(.*)" /> 
            <conditions logicalGrouping="MatchAll" trackAllCaptures="true">
                 <add input="{REQUEST_URI}" pattern="robots.txt$" negate="true" />
                 <add input="{HTTP_USER_AGENT}" pattern="googlebot|Google \(\+https|adsbot-google|mediapartners-google|googleimageproxy|facebot|Twitterbot|facebookexternalhit|ia_archiver|baiduspider|sogou|360Spider|mj12bot|bingbot|simplepie\/|yahoo! slurp|duckduckbot|yandex\.ru\/bots|yandex\.net\/bots|yandex\.com\/bots|Exabot|Slackbot-LinkExpanding|MSOffice|Microsoft Outlook|SkypeUriPreview|LinkedInBot" />
                <add input="{QUERY_STRING}" pattern="(.*)" />   
                <add input="{HTTP_PROTOCOL}" pattern="(.*)" />  

          </conditions> 
            <action type="Rewrite" url="http://localhost:(BackendPort)/api/admin/SimulateSSR?url=http://{HTTP_HOST}/{UrlEncode:{R:0}}?{QUERY_STRING}" appendQueryString="true" logRewrittenUrl="false" />
         </rule>
        */



        [HttpGet("SimulateSSR")]
        [AllowAnonymous]
        public async Task<IActionResult> SimulateSSR([FromQuery] string url)
        {
            try
            {

                // PLEASE READ THE FOLOWING COMMENT FIRST :Yazan

                //SaveInCssFile(HttpUtility.UrlDecode(url), "url");


                var cacheData = _cacheService?.GetData<ContentResult>("SSR" + url);

                if (cacheData != null)
                {
                    // SaveInCssFile(url, "inCache");

                    return cacheData;
                }

                var html = await GetHtml(HttpUtility.UrlDecode(url));

                var result = new ContentResult()
                {
                    ContentType = "text/html",
                    Content = html
                };
                //  SaveInCssFile(html, "res");

                _cacheService.SetData<ContentResult>("SSR" + url, result, DateTimeOffset.Now.AddDays(double.Parse(_configuration.GetSection("CacheConfig:CachePeriod").Value)));

                return result;

            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }


        }

        [NonAction]
        private void CreateIfMissing(string path)
        {
            bool folderExists = Directory.Exists(path);
            if (!folderExists)
                Directory.CreateDirectory(path);
        }


        [HttpPost]
        [Route("ChangePasswordForAdmin")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordForAdminViewModel model)
        {
            try
            {
                await _UserService.ChangePasswordForAdmin(model);
                return Ok();
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [NonAction]
        private async Task<string> GetHtml(string Url)
        {
            ChromeDriver chrome = null;

            string chromePath = _configuration.GetSection("SSR:ChromePath")?.Value.ToString();
            int TimeOutInSecends = Convert.ToInt32(_configuration.GetSection("SSR:TimeOutInSecends")?.Value);
            if (!System.IO.File.Exists(chromePath))
            {
                throw new Exception("Chrome Path");
            }
            try
            {
                if (!System.IO.File.Exists(chromePath))
                {
                    throw new Exception("Chrome Path Not Found");
                }


                var options = new ChromeOptions
                {
                    BinaryLocation = chromePath,
                    LeaveBrowserRunning = false,
                
                };
                options.AddArguments("headless");
                chrome = new ChromeDriver(options);

                chrome.Navigate().GoToUrl(Url);
                // SaveInCssFile(chrome.PageSource, "beforeDelay");
                await Task.Delay(TimeOutInSecends * 1000);

                // SaveInCssFile(chrome.PageSource, "afterDelay");

                string Content = chrome.PageSource;

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(Content);

                foreach (var style in htmlDoc.DocumentNode.SelectNodes("//style"))
                {
                    style.Remove();

                }

                chrome.CloseDevToolsSession();
                chrome.Quit();
                chrome.Dispose();
                return htmlDoc.DocumentNode.InnerHtml;



            }
            catch (Exception e)
            {
                if (chrome is not null)
                {
                    chrome.CloseDevToolsSession();
                    chrome.Quit();
                    chrome.Dispose();

                }
                throw e;
            }

        }

        [NonAction]
        private bool SaveInCssFile(string content, string name)
        {
            try
            {
                string Folder = "wwwroot/log";

                CreateIfMissing(Folder);
                string FileName = Folder + "/" + name + "-" + DateTime.Now.ToString("yyyy-MM--dd-hh-mm--ss") + ".txt";

                if (System.IO.File.Exists(FileName))
                {
                    System.IO.File.Delete(FileName);


                }

                StringBuilder Builder = new StringBuilder();

                Builder.Append(content);

                byte[] bytes = Encoding.UTF8.GetBytes(Builder.ToString());

                using (var stream = System.IO.File.Create(FileName))
                {

                    stream.Write(bytes, 0, bytes.Length);


                }
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }



        private async Task<List<string>> ImportFile(IFormFile file)
        {
            var List = new List<string>();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.Commercial;
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;
                    for (int row = 2; row <= rowCount; row++)
                    {
                        List.Add(
                         worksheet.Cells[row, 1].Value.ToString().Trim()




                        );
                    }
                }
            }
            return List;
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var output = input;
            // 62nd char of encoding
            output = output.Replace('-', '+');
            // 63rd char of encoding
            output = output.Replace('_', '/');
            // Pad with trailing '='s
            switch (output.Length % 4)
            {
                case 0:
                    // No pad chars in this case
                    break;
                case 2:
                    // Two pad chars
                    output += "=="; break;
                case 3:
                    // One pad char
                    output += "="; break;
                default:
                    throw new InvalidOperationException("Illegal base64url string!");
            }

            // Standard base64 decoder
            return Convert.FromBase64String(output);



        }
    }
}
