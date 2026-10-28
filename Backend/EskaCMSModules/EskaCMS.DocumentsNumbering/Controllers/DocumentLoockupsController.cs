using EskaCMS.DocumentsNumbering.Interfaces;
using EskaCMS.DocumentsNumbering.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EskaCMS.Plans.Controllers
{
    [Route("api/DocumentLoockups/[action]")]
    [ApiController]
    [Authorize]

    public class DocumentLoockupsController : ControllerBase
    {
        private readonly IDocumentLoockupsService _documentLoockupsService;
        public DocumentLoockupsController(IDocumentLoockupsService documentLoockupsService)
        {
            _documentLoockupsService = documentLoockupsService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateType([FromBody] DocumentTypeAddVM request)
        {
            try
            {
                return Ok(await _documentLoockupsService.CreateType(request));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateClassification([FromBody] DocumentClassificationAddVM request)
        {
            try
            {
                return Ok(await _documentLoockupsService.CreateClassification(request));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDocumentType(int id)
        {
            try
            {
                return Ok(await _documentLoockupsService.GetDocumentType(id));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        } 
        [HttpGet]
        public async Task<IActionResult> GetAllDocumentTypes()
        {
            try
            {
                return Ok(await _documentLoockupsService.GetAllDocumentTypes());

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDocumentClassification(int id)
        {
            try
            {
                return Ok(await _documentLoockupsService.GetDocumentClassification(id));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAllDocumentClassifications()
        {
            try
            {
                return Ok(await _documentLoockupsService.GetAllDocumentClassifications());

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        } 
        [HttpPost]
        public async Task<IActionResult> GetAuthorizedDepartments([FromBody] GetAllVM request)
        {
            try
            {
                return Ok(await _documentLoockupsService.GetAuthorizedDepartments(request.Username));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        } 
        [HttpGet]
        public async Task<IActionResult> GetAllDepartments()
        {
            try
            {
                return Ok(await _documentLoockupsService.GetAllDepartments());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }  
        [HttpGet]
        public async Task<IActionResult> GetManagementDepartments()
        {
            try
            {
                return Ok(await _documentLoockupsService.GetManagementDepartments());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        } 
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                return Ok(await _documentLoockupsService.GetAllUsers());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetUserAndManager()
        {
            try
            {
                return Ok(await _documentLoockupsService.GetUserAndManager());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetUsersByParentUsername([FromBody] GetAllVM request)
        {
            try
            {
                return Ok(await _documentLoockupsService.GetUsersByParentUsername(request.Username,request.FlagValue));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAllCountries()
        {
            try
            {
                return Ok(await _documentLoockupsService.GetAllCountries());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
