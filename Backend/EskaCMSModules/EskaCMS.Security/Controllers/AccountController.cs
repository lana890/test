using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Manage;
using EskaCMS.Filters.ActionFilters;
using EskaCMS.Filters.Helpers.Interfaces;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Infrastructure.Helpers.HttpHelper;
using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.Extensions.Configuration;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Security.Controllers
{
    [Route("api/Account")]
    public class AccountController : Controller
    {

        // private readonly UserManager<User> _userManager;

        private readonly IUserService _IUserService;
        private readonly IWorkContext _workContext;
        // private readonly IRSA_Helper _IRSA_Helper;
        private readonly IFacebookAuthService _facebookAuthService;
        private readonly IGoogleAuthService googleAuthServices;
        private readonly IAppleAuthServices appleAuthServices;


        public AccountController(
            IUserService IUserService,
            IWorkContext workContext,
            /* IRSA_Helper IRSA_Helper
        , */ IFacebookAuthService facebookAuthService
        , IGoogleAuthService googleAuthServices
        , IAppleAuthServices appleAuthServices/* ,
           UserManager<User> userManager */)
        {
            _IUserService = IUserService;
            _workContext = workContext;
            // _IRSA_Helper = IRSA_Helper;
            _facebookAuthService = facebookAuthService;
            this.googleAuthServices = googleAuthServices;
            this.appleAuthServices = appleAuthServices;
            // _userManager = userManager;
        }


     
        // POST: /Account/Login
        [HttpPost]
        [Route("login")]
        [Authorize]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            try
            {
                return Ok(await _IUserService.Login(model));
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Two factor auth is required"))
                {
                    return new ObjectResult(new { message = "Two factor auth is required", IsTwoFactorEnabled = true })
                    {
                        StatusCode = StatusCodes.Status207MultiStatus
                    };
                }

                return BadRequest(e.Message);
            }
        }
        [HttpPost]

        [Route("ValidateCredentials")]
        [Authorize]
        public async Task<IActionResult> ValidateCredentials([FromBody] LoginViewModel model)
        {
            try
            {
                return Ok(await _IUserService.ValidateCredentials(model));
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Two factor auth is required"))
                {
                    return new ObjectResult(new { message = "Two factor auth is required", IsTwoFactorEnabled = true })
                    {
                        StatusCode = StatusCodes.Status207MultiStatus
                    };
                }

                return BadRequest(e.Message);
            }
        }



        [HttpPost]
        [Route("FacebookLogin")]
        [Authorize]
        public async Task<IActionResult> FacebookLogin([FromBody]FacebookLoginViewModel model)
        {
            try
            {
                return Ok(await _facebookAuthService.Login(model));
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Two factor auth is required"))
                {
                    return new ObjectResult(new { message = "Two factor auth is required" })
                    {
                        StatusCode = StatusCodes.Status207MultiStatus
                    };
                }
                return BadRequest(e.Message); 
            }
        }

        [HttpPost]
        [Route("GoogleLogin")]
        [Authorize]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginViewModel model)
        {
            try
            {
                return Ok(await googleAuthServices.Login(model));
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Two factor auth is required"))
                {
                    return new ObjectResult(new { message = "Two factor auth is required" })
                    {
                        StatusCode = StatusCodes.Status207MultiStatus
                    };
                }
                return BadRequest(e.Message);
            }
        }
        [HttpPost]
        [Route("AppleLogin")]
        [Authorize]
        public async Task<IActionResult> AppleLogin([FromBody] AppleLoginViewModel model)
        {
            try
            {
                return Ok(await appleAuthServices.Login(model));
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Two factor auth is required"))
                {
                    return new ObjectResult(new { message = "Two factor auth is required" })
                    {
                        StatusCode = StatusCodes.Status207MultiStatus
                    };
                }
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        [Route("GetCrawlerToken")]
        public async Task<IActionResult> GetCrawlerToken([FromBody] CrawlerViewModel model)
        {
            try
            {
                return Ok(await _IUserService.GetCrawlerToken(model));

            }
            catch (Exception ex)
            {

               return BadRequest(ex.Message);
            }
        
        }

        [HttpPost]
        [Route("HandShake")]
        public async Task<IActionResult> HandShake()
        {
            try
            {
                var Key = _workContext.GetUserPublicKey();
                var Result = await _IUserService.HandShake(Key);

                Request.Headers.Add("pointerId", Result.PointerId);
                return Ok(Result.CypherParam);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }


        [HttpPost]
        [Route("IsValidPointerId")]
        public async Task<IActionResult> IsValidPointerId()
        {
            try
            {
                var PointerId = Request.Headers["pointerId"];
                return Ok(new { Result = await _IUserService.IsValidPointerId(PointerId) });
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [Route("GetUserImageById/{UserId}")]
        public async Task<IActionResult> GetUserImageById(long UserId)
        {
            try
            {
                return Ok(await _IUserService.GetUserImageById(UserId));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



		[HttpGet]
		[Route("GetUserByUsername")]
		public async Task<User> GetUserImageById(string Username)
		{
			try
			{
				long SiteId = _workContext.GetCurrentSiteId();


				return await _IUserService.GetUserByUserName(SiteId,Username);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}




		[HttpPut("update-last-login")]
        public async Task<IActionResult> UpdateLastLogin()
        {
            try
            {
                await _IUserService.UpdateLastLogin();
                return Ok();
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }
        //
        // POST: /Account/Register
        [HttpPost]
        [Authorize]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            try
            {
                var result = await _IUserService.Register(model);
                return Ok(new { Message = "User created successfully", UserId = EncryptDecryptHelper.Encrypt(result.UserId.ToString())});
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }



        [HttpPost]
        [Authorize]
        [Route("RegisterWithoutPassword")]
        public async Task<IActionResult> RegisterWithoutPassword([FromBody] RegisterWithoutPasswordVM model)
        {
            try
            {
                var result = await _IUserService.Register(new RegisterViewModel()
                {
                    DateOfBirth = model.DateOfBirth,
                    Email = model.Email,
                    FullName = model.FullName,
                    GroupId = model.GroupId,
                    IsADAuthRequired = model.IsADAuthRequired,
                    IsEskaCoreIntegration=model.IsEskaCoreIntegration,
                    PhoneNumber = model.PhoneNumber,
                    RoleId = model.RoleId,
                    SiteId = model.SiteId,
                    Status = EStatus.Inactive,
                    UserName = model.UserName,
                    UserOptions = model.UserOptions,
                    Password="Customer@123"+new Guid().ToString(),
                    
                }
                );
                return Ok(new { Message = "User created successfully", UserId = EncryptDecryptHelper.Encrypt(result.UserId.ToString()) });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }



        [Route("EditProfile")]
        [HttpPut, DisableRequestSizeLimit]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            try
            {
                EditProfileViewModel model = JsonSerializer.Deserialize<EditProfileViewModel>(Request.Form["data"].ToString());

                IFormFile file = null;
                if (Request.Form.Files.Count > 0)
                {
                    file = Request.Form.Files[0];
                }
                await _IUserService.EditProfile(model, file);
                return Ok();

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [Route("AddUpdateUserOptions/{RegistrationId}")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddUpdateUserOptions(string RegistrationId, [FromBody] List<UserOptionsVM> userOptions)
        {
            try
            {
                await _IUserService.AddUpdateUserOptions(RegistrationId, userOptions);
                return Ok();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("CheckUserOptionValidation")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CheckUserOptionValidation([FromBody] List<CheckUserOptionValidationVM> validations)
        {
            try
            {
                await _IUserService.CheckUserOptionValidation(validations);
                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }


        }

        [Route("AddUpdateUserOptions")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddUpdateUserOptions([FromBody] List<UserOptionsVM> userOptions)
        {
            try
            {
                await _IUserService.AddUpdateUserOptions(userOptions);
                return Ok();

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [Route("LogOff")]
        [AllowAnonymous]
        //[Authorize]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            
            await _IUserService.LogOff();
            return Ok(true);
        }


        ////  POST: /Account/ExternalLogin
        //[HttpPost]
        ////[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public IActionResult ExternalLogin(string provider, string returnUrl)
        //{
        //    // Request a redirect to the external login provider.
        //    var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
        //    var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        //    return Challenge(properties, provider);
        //}

        [HttpPost]
        [Route("AddRoleToUser/{RoleId}")]
        public async Task<IdentityResult> AddRoleToUser(long RoleId)
        {
          return  await _IUserService.AddRoleToUser(RoleId);
         //   return Ok();
        }


        // GET: /Account/ConfirmEmail
        [HttpGet]
        [Route("ConfirmEmail")]
        //[AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string code)
        {
            try
            {
                await _IUserService.ConfirmEmail(code);
                return Ok("Account confirmed");
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }


        // POST: /Account/ForgotPassword

        [HttpPost]
        //[AllowAnonymous]
        [Route("ForgotPasswordExternal")]
        //Was a Name In 3.7.3 ForgotPassword
        [Authorize]
        public async Task<IActionResult> ForgotPasswordExternalLink([FromBody] Core.Entities.ForgotPasswordViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _IUserService.ForgotPasswordExternalLink(model);
                    return Ok();
                }
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
            // If we got this far, something failed, redisplay form
            return BadRequest("ForgotPasswordViewModel not valid");
        }

        [HttpPost]
        [Route("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            try
            {
                await _IUserService.ChangePassword(model);
                return Ok();
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);  
            }
        }
        // POST: /Account/ResetPassword
        [HttpPost]
        [Authorize]
        [Route("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] Core.Entities.ResetPasswordViewModel model)
        {
            try
            {
                await _IUserService.ResetPassword(model);
                return Ok();
            }

            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        // POST: /Account/SendCode
        [HttpPost]
        [Route("SendCode")]
        [Authorize]
        public async Task<IActionResult> SendCode([FromBody] SendCodeViewModel model)
        {
            try
            {

                await _IUserService.SendCode(model);
                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
   
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [Authorize]
        [Route("VerifyCode")]

        public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid model !");
            }
            try
            {
                await _IUserService.VerifyCode(model);

                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }


        [HttpPost]
        [AllowAnonymous]
        [Authorize]
        [Route("ActivateUser")]


        public async Task<IActionResult> ActivateUser([FromBody] VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid model !");
            }
            try
            {
                await _IUserService.ActivateUser(model);

                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpGet]
        [Route("getProfile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var profileData = await _IUserService.GetProfile();
                return Json(profileData);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }


        }

        [HttpGet]
        [Route("GetUserTypes")]
        [Authorize]
        public async Task<IActionResult> GetUserTypes()
        {
            try
            {
                return Ok( await _IUserService.GetUserTypes());
               
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Route("GetUsersByGroupId/{groupId}/{pageIndex?}/{pageSize?}")]
        [Authorize]
        public async Task<IActionResult> GetUsersByGroupId(long groupId, int pageIndex = 0, int pageSize = 20)
        {
            try
            {
                return Ok( await _IUserService.GetUsersByGroupId(groupId, pageIndex, pageSize));
            }        
            catch(Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }
        
       

        [HttpGet]
        [Route("GetUserByUserOption/{UserOptionValue}/{UserOptionKey}")]
        [Authorize]
        public async Task<IActionResult> GetUserByUserOption(string UserOptionValue,string UserOptionKey)
        {
            try
            {
                return Ok(await _IUserService.GetUserByUserOption(UserOptionValue, UserOptionKey));

            }
            catch(Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("GetUserInfo")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo([FromBody]UsernameVM model)
        {
            try
            {
                var result = await _IUserService.GetUserInfo(model.Username);
              return Ok(result);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }    
        }

        [HttpPost]
        [Route("GetUserContactInfo")]
        [Authorize]
        public async Task<IActionResult> GetUserContactInfo([FromBody]UsernameVM model)
        {
            try
            {
                long SiteId= await _workContext.GetCurrentSiteIdAsync();
                return Ok(await _IUserService.GetUserContactInfo(model.Username, SiteId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("DeleteAccount")]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountViewModel model)
        {
            try
            {
                await _IUserService.DeleteAccount(model);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetUserOptions")]
        [Authorize]
        public async Task<IActionResult> GetUserOptions()
        {

            try
            {
                return Ok(await _IUserService.GetUserOptionsAsync());
            }
            catch (Exception exc)
            {
                exc = ExceptionHelper.ThrowDeepException(exc);
                return BadRequest(exc.Message);
            }

        }


        [HttpGet]
        [Route("GetUserOptionKeys")]
        [Authorize]
        public async Task<IActionResult> GetUserOptionKeys()
        {

            try
            {
                var result = await _IUserService.GetUserOptionKeys();
                return Ok(new { OptionKeys = result});
            }
            catch (Exception exc)
            {
                exc = ExceptionHelper.ThrowDeepException(exc);
                return BadRequest(exc.Message);
            }

        }
        [HttpPost]
        [Route("SetFavoritePage/{ParentId}")]
            
        public async Task<IActionResult>SetFavoritePage(long ParentId)
        {
            try
            {
                return Ok(await _IUserService.SetFavoritePage(ParentId));
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        [Route("GetPasswordChangesHistory")]
        public async Task<IActionResult> GetPasswordChangesHistory()
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var UserId= await _workContext.GetCurrentUserId();
                return Ok(await _IUserService.GetPasswordChangesHistory(UserId, SiteId));
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        //[HttpGet]
        //[Route("GetUserTokenByUserId/{UserId})]
        //public async Task<IActionResult>GetUserTokenByUserId(long UserId)
        //{
        //    try
        //    {
        //        return Ok(await _IUserService.GetUserTokenByUserId(UserId));
        //    }
        //    catch(Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

        [HttpGet]
        [Route("GetUserTokenByUserIdAndPurpose/{Username}/{Purpose?}")]
        public async Task<IActionResult> GetUserTokenByUserIdAndPurpose(string Username,string Purpose =null)
        {
            try
            {
                return Ok(await _IUserService.GetUserTokenByUserIdAndPurpose(Username, Purpose));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("UploadUserImage")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UploadUserImage([FromForm] UploadImageDto model)
        {
            try
            {
               var result =  await _IUserService.UploadImage(model);

                if (result)
                {
                    return Ok("User image has been uploaded");

                }

                return BadRequest("Faild to upload user image");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("FingerLogin")]
        [HttpPost]
        [Authorize]

        public async Task<IActionResult> FingerLogin([FromBody] FingerVM model)
        {
            try
            {
                return Ok(await _IUserService.FingerLogin(model));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        
        }



        [Route("EnableFingerPrint")]
        [HttpPost]
        [Authorize]

        public async Task<IActionResult> EnableFingerPrint([FromBody] FingerPublicVM model)
        {
            try
            {
                return Ok(await _IUserService.EnableFingerPrint(model));
            }
            catch (Exception ex)
            {

                throw ex;
            }
        
        }


        [Route("DisableFingerPrint")]
        [HttpPut]
        [Authorize]

        public async Task<IActionResult> DisableFingerPrint()
        {
            try
            {
                return Ok(await _IUserService.DisableFingerPrint());
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        [HttpPut]
        [Route("SetUserCulturePreference")]
        [Authorize]
        public async Task<IActionResult> SetUserCulturePreference()
        {
            try
            {

                await _IUserService.SetUserCulturePreference();
                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Authorize]
        [Route("GetRoleOptions")]
        public async Task<IActionResult> GetRoleOptions()
        {
            try
            {
                var result = await _IUserService.GetRoleOptions();
                return Ok(new { RoleOptions = result });

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpPost]
        [Route("SetWarehousePreference")]
        [Authorize]
        public async Task<IActionResult> SetWarehousePreference([FromBody] WarehousePreferenceViewModel model)
        {
            try
            {

                await _IUserService.SetWarehousePreference(model);
                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetCurrentWarehouse")]
        [Authorize]
        public async Task<IActionResult> GetCurrentWarehouse()
        {
            try
            {

                long WarehouseId = await _IUserService.GetCurrentWarehouse();
                return Ok(WarehouseId);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

    }


}
