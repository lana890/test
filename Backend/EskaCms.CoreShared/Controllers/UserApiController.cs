using EskaCms.CoreShared.ViewModels;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.CoreShared.Services;
using EskaCMS.CoreShared.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.CoreShared.Controllers
{
    [Area("Core")]
    [Route("api/users")]
    [Authorize]
    public class UserApiController : Controller
    {
        // private readonly IRepository<User> _userRepository;
        // private readonly UserManager<User> _userManager;
        // private readonly IRepository<UsersSites> _userSitesRepository;
        private readonly IUserApiService _userApiService;
        // private readonly ICurrencyRatesApiService _CurrencyRatesApiService;
        private readonly IWorkContext _workContext;
        private readonly ICurrencyService _currencyService;
        // private readonly IRepository<EskaCMS.Entities.Entity.Core.UserOptions> _UserOptionsRepository;
        public UserApiController(/* IRepository<User> userRepository, */ /* UserManager<User> userManager, */
            /* IRepository<UsersSites> userSitesRepository, */
            /* IRepository<EskaCMS.Entities.Entity.Core.UserOptions> UserOptionsRepository, */
            IUserApiService userApiService,
            ICurrencyService currencyService,
            /* ICurrencyRatesApiService CurrencyRatesApiService, */
            IWorkContext workContext)
        {
            // _userRepository = userRepository;
            // _userManager = userManager;
            // _userSitesRepository = userSitesRepository;
            // _UserOptionsRepository = UserOptionsRepository;
            _userApiService = userApiService;
            // _CurrencyRatesApiService = CurrencyRatesApiService;
            _workContext = workContext;
            _currencyService = currencyService;

        }

        [HttpPost]
        [Route("QuickSearch")]
        public async Task<IActionResult> QuickSearch([FromBody] UserSearchOption searchOption)
        {
            try
            {
                long siteId = await _workContext.GetCurrentSiteIdAsync();

                return Ok(await _userApiService.QuickSearch(searchOption, siteId));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }




        [HttpGet]
        [Route("SyncADUsers")]
        public async Task<IActionResult> SyncADUsers()
        {
            try
            {
                bool DidSyncExc = await _userApiService.SyncADUsers();

                if (DidSyncExc)
                    return Ok(DateTime.Now.ToString("dd-MM-yyyy"));

                return BadRequest("Internal server Error");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpGet]
        [Route("GetUserTypes")]
        public IActionResult GetUserTypes()
        {
            var enumVals = new List<object>();

            foreach (var item in Enum.GetValues(typeof(EUsersTypes)))
            {

                enumVals.Add(new
                {
                    id = (int)item,
                    name = item.ToString()
                });
            }

            return Ok(enumVals);
        }


        [HttpGet]
        [Route("GetUser/{id}")]
        public async Task<IActionResult> GetUser(long id)
        {
            try
            {
                return Ok(await _userApiService.GetById(id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetUserChildren/{id?}")]
        public async Task<IActionResult> GetUserChildren([FromRoute] long? id)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();
                if (id.HasValue&& id!=0)
                {
                    userId=id.Value;

                }
                return Ok(await _userApiService.GetUserChildren(userId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetUserCode/{UserId}")]
        [Route("GetUserCode")]
        public async Task<IActionResult> GetUserCode([FromRoute] long? UserId)
        {
            try
            {
                var Id = UserId.HasValue ? UserId.Value : await _workContext.GetCurrentUserId();
                return Ok(await _userApiService.GetUserCode(Id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("CheckReferrerCode")]

        public async Task<IActionResult> CheckReferrerCode([FromBody] ReferrerCodeVM code)
        {
            try
            {
                var result = await _userApiService.CheckReferrerCode(code);
                if (result)
                {
                    return Ok(result);

                }

                else
                {
                    return BadRequest("Invalid Code");
                }

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetDefaultUserCurrency")]
        public async Task<IActionResult> GetDefaultUserCurrency()
        {
            try
            {
                var result = await _currencyService.GetDefaultUserCurrency();


                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [Route("EditUserCountry")]
        [HttpPut]
        public async Task<IActionResult> EditUserCountry([FromBody] EditUserCountry model)
        {
            try
            {

                var result = await _userApiService.EditUserCountry(model);

                return Ok(result);
            }
            catch (Exception E)
            {
                return BadRequest();
            }
        }


        [HttpGet]
        [Route("GetDefaultUserCountry")]
        public async Task<IActionResult> GetDefaultUserCountry()
        {
            try
            {
                var result = await _userApiService.GetUserCountry();


                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }





        [Route("AddUser")]
        [HttpPost, DisableRequestSizeLimit]
        public async Task<IActionResult> AddUser()
        {
            try
            {
                UserForm model = JsonSerializer.Deserialize<UserForm>(Request.Form["data"].ToString());
                long siteId = long.Parse(Request.Headers["siteId"].ToString());

                return Ok(await _userApiService.Create(model, siteId));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("UploadUsers")]
        public async Task<IActionResult> UploadUsers([FromForm] IFormFile formFile, [FromQuery] long RoleId, [FromQuery] long CustomerGroupId)
        {
            try
            {
                UploadUsers uploadUsers = new UploadUsers();

                uploadUsers.RoleIds.Add(RoleId);

                uploadUsers.CustomerGroupIds.Add(CustomerGroupId);

                uploadUsers.SiteIds.Add(_workContext.GetCurrentSiteId());


                var UsersInfo = await _userApiService.UploadUsers(formFile, uploadUsers);
                return Ok();

            }



            catch (Exception exc)
            {

                return StatusCode(StatusCodes.Status400BadRequest, exc.Message);
            }
        }




        [Route("EditUser/{id}")]
        [HttpPut, DisableRequestSizeLimit]
        public async Task<IActionResult> EditUser(long id)
        {
            try
            {
                UserForm model = JsonSerializer.Deserialize<UserForm>(Request.Form["data"].ToString());
                long SiteId = long.Parse(Request.Headers["siteId"].ToString());
                IFormFileCollection files = Request.Form.Files;
                return Ok(await _userApiService.Update(model, SiteId, id, files));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

        }


        [Route("EditUserCurrency")]
        [HttpPut]
        public async Task<IActionResult> EditUserCurrency([FromBody] EditUserCurrencyVM model)
        {
            try
            {

                await _userApiService.EditUserCurrencyAndCulture(model);

                return Ok();

            }
            catch (Exception E)
            {
                return BadRequest(E.Message);
            }
        }


        [Route("EditUserOption")]
        [HttpPut]
        public async Task<IActionResult> EditUserOption([FromBody] UserOptionsVM userOptionsVM)
        {
            try
            {
                await _userApiService.EditUserOption(userOptionsVM);
                return Ok("edit successfully"
                    );
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [Route("GetAllUsersWithOptions")]
        [HttpPost]
        public async Task<IActionResult> GetAllUsersWithOptions([FromBody] GetUserOptionByPropertyVM model)
        {
            try
            {
                var result = await _userApiService.GetAllUsersOptions(model);
                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [HttpDelete]
        [Route("deleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(long id)
        {
            try
            {
                //UserForm model = JsonSerializer.Deserialize<UserForm>(Request.Form["data"].ToString());
                await _userApiService.Delete(id);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpDelete]
        [Route("deleteUserOption/{id}")]
        public async Task<IActionResult> DeleteUserOption(long id)
        {
            try
            {
                await _userApiService.DeleteUserOption(id);
                return Ok("Deleted Successfuly");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        [Route("GetUsersDDL")]
        public async Task<IActionResult> GetUsersDDL([FromBody] UserSearchTableParam<UserGetddlVM> param)
        {
            try
            {

                return Ok(await _userApiService.GetUsersDDL(param));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }



        [HttpPost]
        [Route("GetList")]
        public async Task<IActionResult> GetList([FromBody] UserSearchTableParam<UserListSearchVM> param, FillterType? fillterType)
        {
            try
            {

                return Ok(await _userApiService.GetList(param, fillterType));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        [Route("GetListWithAdvanceSearch")]
        public async Task<IActionResult> GetListWithAdvanceSearch([FromBody] UserSearchTableParam<UserListAdvanceSearchVM> param)
        {
            try
            {

                return Ok(await _userApiService.GetListWithAdvancedSearch(param));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpPost]
        [Route("ExportUsersData")]
        public async Task<IActionResult> ExportUsersData([FromBody] UserSearchTableParam<UserListSearchVM> param, FillterType? fillterType)
        {
            try
            {
                var data = await _userApiService.ExportUsers(param, fillterType);

                var file = await ExcelHelper.ExportExcelFile(data, "UsersFile");
                return File(file.FileBytes, file.ContentType, file.Name);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpGet]
        [Route("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {

                return Ok(await _userApiService.GetAll());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [Route("GetUserDetails/{UserId}")]
        public async Task<IActionResult> GetUserDetails(long UserId)
        {
            try
            {
                return Ok(await _userApiService.GetById(UserId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("BasicInfo/{UserId}")]
        public async Task<IActionResult> BasicInfo([FromRoute] long UserId)
        {
            try
            {
                return Ok(await _userApiService.GetBasicinfo(UserId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("UpdateBasicInfo")]
        [Route("UpdateBasicInfo/{UserId}")]
        public async Task<IActionResult> UpdateBasicInfo([FromBody] UserBasicInfoVM model, [FromRoute] long? UserId)
        {
            try
            {
                if (!UserId.HasValue)
                {
                    UserId = long.Parse(EncryptDecryptHelper.Decrypt(model.UserId));
                }
                return Ok(await _userApiService.UpdateBasicinfo(model, UserId.Value));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet("AvailabilityStatus")]
        public IActionResult AvailabilityStatus()
        {
            var result = new List<object>();

            foreach (var item in Enum.GetValues(typeof(EUserAvailabilityStatus)))
            {

                result.Add(new
                {
                    Value = (int)item,
                    Label = ((EUserAvailabilityStatus)item).GetDisplayName()
                });



            }

            return Ok(result);

        }



        [HttpPost]
        [Route("GetAllEmployees")]
        public async Task<IActionResult> GetAllEmployees([FromBody] FilterUserVM model)
        {
            try
            {
                return Ok(await _userApiService.GetAllEmployees(model));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }





    }
}

