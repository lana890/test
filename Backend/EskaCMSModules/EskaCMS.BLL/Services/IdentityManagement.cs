//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;

//using EskaCMS.Core.Services;
//using EskaCMS.Entities.Entity;

//using EskaCMS.Core.Extentions.Settings;
//using EskaCMS.Core.Manage;
//using EskaCommerce.Core.Manage;
//using EskaCMS.Infrastructure.Data;
//
//using System.Dynamic;
//using EskaCMS.EmailSender.Services.Interfaces;
//using Microsoft.EntityFrameworkCore;
//using System;


//namespace EskaCMS.BLL.Services
//{
//    public class IdentityManagement
//    {
//        private readonly UserManager<User> _userManager;
//        private readonly SignInManager<User> _signInManager;
//        private readonly IEmailSender _emailSender;
//        private readonly ISmsSender _smsSender;
//        //  private readonly ILogger _logger;
//        private readonly SettingDefinitionProvider _settingDefinitionProvider;
//        private readonly ISettingService _settingService;


//        private readonly IRepository<Policies> _PoliciesRepository;
//        private readonly IRepository<RoleClaims> _RoleClaimsRepository;
//        private readonly IRepository<Claims> _ClaimsRepository;

//        private readonly RoleManager<Role> _roleManager;



//        public IdentityManagement(
//        UserManager<User> userManager,
//        SignInManager<User> signInManager,
//        IEmailSender emailSender,
//        ISmsSender smsSender,
//        // ILoggerFactory loggerFactory,
//        SettingDefinitionProvider settingDefinitionProvider,
//        ISettingService settingService,
//        IRepository<Policies> PoliciesRepository,
//        IRepository<RoleClaims> RoleClaimsRepository,
//        IRepository<Claims> ClaimsRepository,
//        RoleManager<Role> roleManager)
//        {
//            _userManager = userManager;
//            _signInManager = signInManager;
//            _emailSender = emailSender;
//            _smsSender = smsSender;
//            // _logger = loggerFactory.CreateLogger<ManageController>();
//            _settingDefinitionProvider = settingDefinitionProvider;
//            _settingService = settingService;
//            _PoliciesRepository = PoliciesRepository;
//            _RoleClaimsRepository = RoleClaimsRepository;
//            _ClaimsRepository = ClaimsRepository;
//            _roleManager = roleManager;
//            _userManager = userManager;
//        }
//        public async Task<IActionResult> GetClaims()
//        {
//            dynamic runTimeObject = new ExpandoObject();
//            long SiteId = 1;
//            var Claims = await _ClaimsRepository.Query().ToListAsync();
//            var Polices = _PoliciesRepository.Query().Where(x => x.SiteId == SiteId).ToList();

//            runTimeObject.Claims = Claims;
//            runTimeObject.Polices = Polices;

//            return runTimeObject;
//        }


//        public async Task<UserInfoVM> LoadUserInfo()
//        {
         

//            var user = await GetCurrentUserAsync();
//            var model = new UserInfoVM
//            {
//                HasPassword = await _userManager.HasPasswordAsync(user),
//                PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
//                TwoFactor = await _userManager.GetTwoFactorEnabledAsync(user),
//                Logins = await _userManager.GetLoginsAsync(user),
//                BrowserRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user),
//                FullName = user.FullName,
//                Email = user.Email
//            };
//            return model;
//        }


//        [HttpGet("user/info")]
//        public async Task<UserInfoVm> UserInfo()
//        {
//            var user = await GetCurrentUserAsync();
//            var model = new UserInfoVm { Email = user.Email, FullName = user.FullName };
//            return model;
//        }
//        public async Task<UserSettingViewModel> UserSettings()
//        {
//            var model = new UserSettingViewModel
//            {
//                SettingDefinitions = _settingDefinitionProvider.SettingDefinitions,
//                UserSettings = await _settingService.GetAllSettingsAsync()
//            };

//            return model;
//        }


//        [HttpPost("user/change-password")]
//        [ValidateAntiForgeryToken]
//        public async Task<bool> ChangePassword(ChangePasswordViewModel model)
//        {

//            var user = await GetCurrentUserAsync();
//            if (user != null)
//            {
//                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
//                if (result.Succeeded)
//                {
//                    await _signInManager.SignInAsync(user, isPersistent: false);
//                    //  _logger.LogInformation(3, "User changed their password successfully.");

//                }
              
//                return true;
//            }
//            return false;
//        }

//        public async Task<bool> SetPassword(SetPasswordViewModel model)
//        {
//            var user = await GetCurrentUserAsync();
//            if (user != null)
//            {
//                var result = await _userManager.AddPasswordAsync(user, model.NewPassword);
//                if (result.Succeeded)
//                {
//                    await _signInManager.SignInAsync(user, isPersistent: false);
//                    return true;
//                }
             
//                return false;
//            }
//            return false;
//        }
//        public async Task<ManageLoginsViewModel> ManageLogins()
//        {
//            var user = await GetCurrentUserAsync();
//            if (user == null)
//            {
//                throw new Exception("User name is not exist");
//            }
//            var userLogins = await _userManager.GetLoginsAsync(user);
//            var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
//            var otherLogins = schemes.Where(auth => userLogins.All(ul => auth.Name != ul.LoginProvider)).ToList();
//            var model = new ManageLoginsViewModel
//            {
//                CurrentLogins = userLogins,
//                OtherLogins = otherLogins
//            };
//            return model;

//        }
//        private Task<User> GetCurrentUserAsync()
//        {
//            return _userManager.GetUserAsync(HttpContext.User);
//        }

      
//        public async Task<dynamic> Login([FromBody]LoginViewModel model)
//        {
//                // This doesn't count login failures towards account lockout
//                // To enable password failures to trigger account lockout, set lockoutOnFailure: true

//                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
//                if (result.Succeeded)
//                {
//                    dynamic runTimeObject = new ExpandoObject();
//                    var user = await _userManager.FindByEmailAsync(model.Email);
//                    runTimeObject.UserInfo = user;

//                    var roles = await _userManager.GetRolesAsync(user);
//                    if (roles.Count > 0)
//                    {
//                        var Role = await _roleManager.FindByNameAsync(roles[0]);
//                       // var Claims = _RoleClaimsRepository.Query().Where(x=>x.site)
//                         var Claims = await _RoleClaimsRepository.Query().Where(x => x.RoleId == Role.Id).ToListAsync();
               

//                    runTimeObject.RoleInfo = Claims;
//                    }

//                    return runTimeObject;
//                }
//                else
//                {

//                    return "Not Authrized";
//                }
//            }

       
//        public async Task<bool> Register([FromBody]RegisterViewModel model)
//        {

//                var user = new User { UserName = model.Email, Email = model.Email, FullName = model.FullName, CreatedBy = model.FullName, CreationDate = DateTimeOffset.Now, SiteId = model.SiteId };
//                var result = await _userManager.CreateAsync(user, model.Password);
//            if (result.Succeeded)
//            {
//                await _userManager.AddToRoleAsync(user, "Admin");

//                await _signInManager.SignInAsync(user, isPersistent: false);

//                return true;
//            }
//            else
//                return false;
//        }

       
//        public async Task<bool> LogOff()
//        {
//            await _signInManager.SignOutAsync();
         
//            return true;
//        }




     

//        // GET: /Account/ConfirmEmail
//        [HttpGet]
//        [AllowAnonymous]
//        public async Task<string> ConfirmEmail(string userId, string code)
//        {
//            if (userId == null || code == null)
//            {
//                throw new Exception("User or code is reqiured");
//            }
//            var user = await _userManager.FindByIdAsync(userId);
//            if (user == null)
//            {
//                throw new Exception("User or code is reqiured");
//            }
//            var result = await _userManager.ConfirmEmailAsync(user, code);
//            return result.Succeeded ? "ConfirmEmail" : "Error";
//        }

      
//        //
//        // POST: /Account/ForgotPassword
//        [HttpPost]
//        [AllowAnonymous]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
//        {
            
//                var user = await _userManager.FindByNameAsync(model.Email);
//                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
//                {
//                    // Don't reveal that the user does not exist or is not confirmed
//                    return View("ForgotPasswordConfirmation");
//                }

//                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
//                // Send an email with this link
//                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
//                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
//                await _emailSender.SendEmailAsync(model.Email, "Reset Password",
//                   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
//                return View("ForgotPasswordConfirmation");
            

//            // If we got this far, something failed, redisplay form
//            return View(model);
//        }

//        //
//        // POST: /Account/ResetPassword
//        [HttpPost]
//        [AllowAnonymous]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
//        {
//            //if (!ModelState.IsValid)
//            //{
//            //    return BadRequest();
//            //}
//            var user = await _userManager.FindByNameAsync(model.Email);
//            if (user == null)
//            {
//                // Don't reveal that the user does not exist
//                return BadRequest();
//            }
//            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
//            if (result.Succeeded)
//            {
//                return Ok();
//            }
//            AddErrors(result);
//            return BadRequest(ModelState);
//        }

//        //
//        // POST: /Account/SendCode
//        //[HttpPost]
//        //[AllowAnonymous]
//        //[ValidateAntiForgeryToken]
//        //public async Task<IActionResult> SendCode(SendCodeViewModel model)
//        //{
//        //    if (!ModelState.IsValid)
//        //    {
//        //        return View();
//        //    }

//        //    var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
//        //    if (user == null)
//        //    {
//        //        return View("Error");
//        //    }

//        //    // Generate the token and send it
//        //    var code = await _userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
//        //    if (string.IsNullOrWhiteSpace(code))
//        //    {
//        //        return View("Error");
//        //    }

//        //    var message = "Your security code is: " + code;
//        //    if (model.SelectedProvider == "Email")
//        //    {
//        //        await _emailSender.SendEmailAsync(await _userManager.GetEmailAsync(user), "Security Code", message);
//        //    }
//        //    else if (model.SelectedProvider == "Phone")
//        //    {
//        //        await _smsSender.SendSmsAsync(await _userManager.GetPhoneNumberAsync(user), message);
//        //    }

//        //    return RedirectToAction(nameof(VerifyCode), new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
//        //}

//        ////
//        //// POST: /Account/VerifyCode
//        //[HttpPost]
//        //[AllowAnonymous]
//        //[ValidateAntiForgeryToken]
//        //public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
//        //{
//        //    if (!ModelState.IsValid)
//        //    {
//        //        return View(model);
//        //    }

//        //    // The following code protects for brute force attacks against the two factor codes.
//        //    // If a user enters incorrect codes for a specified amount of time then the user account
//        //    // will be locked out for a specified amount of time.
//        //    var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
//        //    if (result.Succeeded)
//        //    {
//        //        return RedirectToLocal(model.ReturnUrl);
//        //    }
//        //    if (result.IsLockedOut)
//        //    {
//        //        _logger.LogWarning(7, "User account locked out.");
//        //        return View("Lockout");
//        //    }
//        //    else
//        //    {
//        //        ModelState.AddModelError(string.Empty, "Invalid code.");
//        //        return View(model);
//        //    }
//        //}

//        #region Helpers

//        private void AddErrors(IdentityResult result)
//        {
//            foreach (var error in result.Errors)
//            {
//                ModelState.AddModelError(string.Empty, error.Description);
//            }
//        }

//        private Task<User> GetCurrentUserAsync()
//        {
//            return _userManager.GetUserAsync(HttpContext.User);
//        }
//        [HttpGet]
//        [Route("GetAllUsers")]
//        public List<User> GetAllUsers()
//        {
//            try
//            {
//                return _userManager.Users.ToList();
//            }
//            catch (Exception exc)
//            {

//                throw exc;
//            }
//        }
//        //[HttpGet]
//        //[Route("GetAllRoleClaims")]
//        //public void GetAllRoleClaims()
//        //{
//        //    IdentityManagement obj = new IdentityManagement();
//        //    dynamic runTimeObject = new ExpandoObject();
//        //    obj.GetAllRoleClaims(_roleManager);
//        //}
//        //private IActionResult RedirectToLocal(string returnUrl)
//        //{
//        //    if (Url.IsLocalUrl(returnUrl))
//        //    {
//        //        return Redirect(returnUrl);
//        //    }
//        //    else
//        //    {
//        //        return RedirectToAction(nameof(HomeController.Index), "Home");
//        //    }
//        //}

//        #endregion
//    }
//}
