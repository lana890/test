using EskaCMS.Core;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Controllers
{
    [Area("Security")]
    [Route("api/RolesManagement")]
    [ApiController]
    [DisplayName("Role Managment")]
    [Authorize]
    public class RolesManagementController : ControllerBase
    {

        private readonly IRoleService _RoleService;
        private readonly IOptionService _OptionService;
        private readonly IRoleOptions _RoleOptionsService;
        private readonly IRoleAccessService _RoleAccessService;
        private readonly IWorkContext _workContext;
        private readonly IUserRoleService _userRoleService;
        public RolesManagementController(IRoleService RoleServices,
                                        IOptionService OptionServices, 
                                        IRoleOptions RoleOptionsService,
                                        IRoleAccessService RoleAccessService,
                                        IUserRoleService userRoleService,
                                        IWorkContext workContext)
        {
            _RoleService = RoleServices;
            _OptionService = OptionServices;
            _RoleOptionsService = RoleOptionsService;
            _RoleAccessService = RoleAccessService;
            _workContext = workContext;
            _userRoleService = userRoleService;
        }


        [HttpGet("GetOptionsLevelsOfUserRole/{CategoryTypeId?}")]
        [Authorize]
        public async Task<IActionResult> GetOptionsLevelsOfUserRole([FromRoute] long? CategoryTypeId)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                var UserId = await _workContext.GetCurrentUserId();
                var UserOptions = await _RoleOptionsService.GetByUserIdAsync(UserId);
                var categoryTypeId= CategoryTypeId==0 ||CategoryTypeId==null ? (long)ECategoryTypes.RoleOptions:CategoryTypeId.Value ;

                if (UserOptions == null)
                {
                    return Ok(await _OptionService.GetOptionsLevels(categoryTypeId, SiteId));

                }
                var UserOptionsList = UserOptions.lstOptions.Select(l => l.OptionId).ToList();
                return Ok(await _OptionService.GetOptionsLevels(categoryTypeId, SiteId, UserOptionsList));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetRoleOptionsByUserId")]
        public async Task<IActionResult> GetRoleOptionsByUserId()
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();

                var Options = await _RoleOptionsService.GetByUserIdAsync(UserId);

                return Ok(new { Options = Options.lstOptions });

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetOptionsLevels/{CategoryTypeId}")]
        public async Task<IActionResult> GetOptionsLevels([FromRoute] long CategoryTypeId)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                return Ok(await _OptionService.GetOptionsLevels(CategoryTypeId, SiteId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("AddLevelOption")]
        public async Task<IActionResult> AddLevelOption(AddOptionViewModel options)
        {
            try
            {
                options.SiteId = long.Parse(Request.Headers["siteId"].ToString());
                options.UserId = await _workContext.GetCurrentUserId();
                return Ok(await _OptionService.CreateAsync(options));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("EditOption")]
        public async Task<IActionResult> EditOption(AddOptionViewModel options)
        {
            try
            {
                options.SiteId = long.Parse(Request.Headers["siteId"].ToString());
                options.UserId = await _workContext.GetCurrentUserId();
                return Ok(await _OptionService.EditOption(options));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("AddRole")]
        [DisplayName("Add Role")]
        public async Task<IActionResult> AddRole(AddRoleOptionsViewModel role)
        {
            try
            {
                role.SiteId = long.Parse(Request.Headers["siteId"].ToString());
                role.UserId = await _workContext.GetCurrentUserId();
                return Ok(await _RoleService.CreateAsync(role));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }



        [HttpPost]
        [Route("EditRole")]
        [DisplayName("Edit Role")]
        public async Task<IActionResult> EditRole(AddRoleOptionsViewModel role)
        {
            try
            {
                role.SiteId = long.Parse(Request.Headers["siteId"].ToString());
                role.UserId = await _workContext.GetCurrentUserId();
                return Ok(await _RoleService.EditAsync(role));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
      
        [Route("DeleteRole/{id}")]
        [DisplayName("Delete Role"), Authorize]
        public async Task<IActionResult> DeleteRole(long id)
        {
            try
            {
                return Ok(await _RoleService.DeleteAsync(id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Route("GetRoles")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                var UserId = await _workContext.GetCurrentUserId();
                return Ok(await _RoleService.GetBySiteId(SiteId, UserId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }




        [HttpGet]
        [Route("GetRoleById/{id}")]
        public async Task<IActionResult> GetRoleById(long id)
        {
            try
            {
                //  var SiteId = long.Parse(Request.Headers["siteId"].ToString()); AddRoleOptionsViewModel
                var rols = await _RoleService.GetById(id);
                var Options = await _RoleOptionsService.GetAsync(id);

                return Ok(new { role = rols, Options = Options.lstOptions });

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("SaveOptionsForRole")]
        public async Task<IActionResult> SaveOptionsForRole(AddRoleOptionsViewModel Roleoptions)
        {
            try
            {
                return Ok(await _RoleOptionsService.CreateAsync(Roleoptions));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }


        [HttpGet]
        [Route("GetControllersInfo")]
   //     [DisplayName("Get Controller Information")]
        public async Task<IActionResult> GetControllersInfo()
        {
            try
            {
                return Ok(await _RoleService.GetControllersInfoAsync());
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetRoleAccess/{roleId}")]
        [DisplayName("Get Role Access Information")]
        public async Task<IActionResult> GetRoleAccess(long roleId)
        {
            try
            {
                return Ok(await _RoleAccessService.GetRoleAccessAsync(roleId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Authorize]
        [Route("GetAllRoleByRoleType/{RoleType}")]
        public async Task<IActionResult> GetAllRoleByRoleType([FromRoute] int RoleType)
        {
            try
            {


                var result = await _userRoleService.GetAllRolesByType(RoleType);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }


    }
}
