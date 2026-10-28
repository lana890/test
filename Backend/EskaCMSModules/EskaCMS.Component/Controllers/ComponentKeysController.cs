using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Core.ViewModels.ComponentKeys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Controllers
{
    [ApiController]
    [Route("api/ComponentKeys")]
    [Authorize]
    public class ComponentKeysController : ControllerBase
    {
        private readonly IComponentKeysServices _componentKeysServices;
        public ComponentKeysController(IComponentKeysServices componentKeysServices)
        {
            _componentKeysServices = componentKeysServices;


        }

        [HttpGet]
        [Route("GetComponentKeys/{Status?}")]
        public async Task<IActionResult> GetComponentKeys([FromRoute] bool? Status = null)
        {
            try
            {
                return Ok(await _componentKeysServices.GetComponentKeys(Status));

            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpGet]
        [Route("GetComponentKeyById/{Id}")]
        public async Task<IActionResult> GetComponentKeyById([FromRoute] long Id)
        {
            try
            {
                return Ok(await _componentKeysServices.GetComponentKeysById(Id));

            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }


        [HttpPost]
        [Route("UpsertComponentKeys")]
        public async Task<IActionResult> UpsertComponentKeys([FromBody] List<UpsertComponentKeysVM> Model)
        {
            try
            {
                return Ok(await _componentKeysServices.UpsertComponentKeys(Model));

            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }
        [HttpDelete]
        [Route("DeleteComponentKey/{Id}")]
        public async Task<IActionResult> DeleteComponentKey([FromRoute] long Id)
        {
            try
            {
                return Ok(await _componentKeysServices.DeleteComponentKey(Id));

            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpPut]
        [Route("ChangeStatus/{PageId}")]

        public async Task<IActionResult> ChangeStatus([FromRoute] long PageId)

        {
            try
            {
                return Ok(await _componentKeysServices.ChangeStatus(PageId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
            
            
            }


        [HttpPut]
        [Route("ChangeStatusByPageTemplateId/{TemplateId}")]

        public async Task<IActionResult> ChangeStatusByPageTemplateId([FromRoute] long TemplateId)

        {
            try
            {
                return Ok(await _componentKeysServices.ChangeStatusByPageTemplateId(TemplateId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }


        }

    }
}
