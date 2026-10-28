using EskaCMS.Core.Models;
using EskaCMS.DocumentsNumbering.Interfaces;
using EskaCMS.DocumentsNumbering.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EskaCMS.Plans.Controllers
{
    [Route("api/DocumentNumber/[action]")]
    [ApiController]
    [Authorize]

    public class DocumentNumberController : ControllerBase
    {
        private readonly IDocumentNumberService _documentNumberService;
        public DocumentNumberController(IDocumentNumberService documentNumberService)
        {
            _documentNumberService = documentNumberService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DocumentsNumbersAddVM request)
        {
            try
            {
                DocumentsNumbersResponseVM response = await _documentNumberService.Create(request);
                return Ok(response);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetDocumentAuthorizedBy()
        {
            try
            {
                return Ok(await _documentNumberService.GetDocumentAuthorizedBy());

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> LinkSoftCopy([FromBody] LinkSoftCopyVM request)
        {
            try
            {
                await _documentNumberService.LinkSoftCopy(request);
                return Ok();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch]
        public async Task<IActionResult> Update([FromBody] DocumentsNumbersUpdateVM request)
        {
            try
            {
                return Ok(await _documentNumberService.Update(request));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetList([FromBody] ListRequestVM<DocumentsNumbersListRequestVM> request)
        {
            try
            {
                return Ok(await _documentNumberService.GetList(request));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{withManager}")]
    
        public async Task<IActionResult> GetUserPersonnel([FromRoute]bool withManager)
        {
            try
            {
                return Ok(await _documentNumberService.GetUserPersonnel(withManager));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Get(GetByIdVM Model)
        {
            try
            {
                return Ok(await _documentNumberService.Get(Model.Id));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetUserLatestDocumentNo()
        {
            try
            {
                return Ok(await _documentNumberService.GetUserLatestDocumentNo());

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
