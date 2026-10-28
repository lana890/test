using EskaCMS.Chat.DTO;
using EskaCMS.Chat.Services;
using EskaCMS.Chat.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EskaCMS.Chat.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[action]")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageServices messageServices;

        public MessagesController( IMessageServices messageServices)
        {
            this.messageServices = messageServices;
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage([FromBody] CreateMassageDTO model)
        {
            try
            {

               return Ok(await messageServices.CreateMessage(model));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser([FromBody] MessageParams messageParams)
        {

            try
            {
                return Ok(await messageServices.GetMessagesForUser(messageParams));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
          
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage([FromRoute]int id)
        {
            try
            {
                await messageServices.DeleteMessage(id);
                return Ok();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


    }

}
