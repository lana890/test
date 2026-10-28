using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.General.Services.Interfaces;
using EskaCMS.General.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.General.Controllers
{
    [ApiController]
    [Route("api/LookupAPI")]
    public class LookupAPIController : ControllerBase
    {

        private readonly ILookupServices _lookupServices;
        private readonly IWorkContext _workContext;
        public LookupAPIController(ILookupServices lookupServices, IWorkContext workContext)
        {
            _lookupServices = lookupServices;
            _workContext = workContext;
        }

        [HttpPost]
        [Route("GetLookupsList")]
        public async Task<IActionResult> GetLookupsList([FromBody] LookupSearchVM searchParamsVM)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var result = await _lookupServices.GetLookupsList(searchParamsVM, SiteId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [HttpPost]
        [Route("ChangeStatus/{LookupId}/{eStatus}")]
        public async Task<IActionResult> ChangeStatus(long LookupId, EStatus eStatus)
        {
            try
            {

                return Ok(await _lookupServices.ChangeStatus(LookupId, eStatus));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [HttpPost]
        [Route("AddNewLookups")]
        public async Task<IActionResult> AddNewLookups([FromBody] LookupModel lookupModel)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
               
                return Ok(await _lookupServices.AddNewLookups(lookupModel, SiteId));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [HttpPost]
        [Route("EditLookups/{Id}")]
        public async Task<IActionResult> EditLookups([FromBody] LookupModel lookupModel, long Id)
        {
            try
            {
                return Ok(await _lookupServices.EditLookups(lookupModel, Id));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        [HttpPost]
        [Route("DeleteLookup/{Id}")]
        public async Task<IActionResult> DeleteLookup(long Id)
        {
            try
            {
                return Ok(await _lookupServices.DeleteLookup(Id));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        [HttpPost]
        [Route("AddLookupValues")]
        public async Task<IActionResult> AddLookupValues([FromBody] LookupValueVM lookupValuesVM)
        {
            try
            {
                return Ok(await _lookupServices.AddLookupValues(lookupValuesVM));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message.ToString());
            }
        }
        [HttpPost]
        [Route("EditLookupValues/{Id}")]
        public async Task<IActionResult> EditLookupValues([FromBody] LookupValuesWithoutOrder lookupValuesVM, long Id)
        {
            try
            {
                return Ok(await _lookupServices.EditLookupValues(lookupValuesVM, Id));

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        [HttpPost]
        [Route("ChangeLookupValuesStatus/{Id}/{eStatus}")]
        public async Task<IActionResult> ChangeLookupValuesStatus(long Id, EStatus eStatus)
        {
            try
            {
                return Ok(await _lookupServices.ChangeLookupValuesStatus(Id,eStatus));

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        [HttpPost]
        [Route("getLookupValuesbyLookupIdAndvalueId/{lookupId}/{ValueId}")]
        public async Task<IActionResult> getLookupValuesbyLookupIdAndvalueId(long lookupId, long ValueId)
        {
            try
            {
                return Ok(await _lookupServices.getLookupValuesbyLookupIdAndvalueId(lookupId, ValueId));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        [Route("GetLookupValuesByRoles/{RoleIds}")]
        public async Task<IActionResult> GetLookupValuesByRoles(List<long> RoleIds)
        {
            try
            {
                return Ok(await _lookupServices.GetLookupValuesByRoles(RoleIds));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        [HttpPost]
        [Route("UpsertLookupValuesRoles")]
        public async Task<IActionResult> UpsertLookupValuesRoles([FromBody] LookupValuesRolesVM lookupRolesVM)
        {
            try
            {
                return Ok(await _lookupServices.UpsertLookupValuesRoles(lookupRolesVM));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        [HttpGet]
        [Route("GetAllLookupValues/{LookupId}")]
        public async Task<IActionResult> GetAllLookupValues(long LookupId)
        {
            try
            {
                return Ok(await _lookupServices.GetAllLookupValues(LookupId));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [Route("ChangeLookupOrder")]
        public async Task<IActionResult> ChangeLookupOrder(LookupValuesWithOrder Model)
        {
            try
            {
                return Ok(await _lookupServices.ChangeLookupOrder(Model));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        [HttpGet]
        [Route("GetAllLookupValuesByLookupId/{LookupId}")]
        public async Task<IActionResult> GetAllLookupValuesByLookupId(long LookupId)
        {
            try
            {
                return Ok(await _lookupServices.GetAllLookupValuesByLookupId(LookupId));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        [Route("GetRoleForLookupValues/{LookupvalueId}")]
        public async Task<IActionResult> GetRoleForLookupValues(long LookupvalueId)
        {
            try
            {
                return Ok(await _lookupServices.GetRoleForLookupValues(LookupvalueId));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [Route("GetLookupValuesByLookupCode")]
        public async Task<IActionResult> GetLookupValuesByLookupCode([FromBody] LookupCodeDto model)
        {
            try
            {
                return Ok(await _lookupServices.GetAllLookupValuesByCode(model.Code));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
