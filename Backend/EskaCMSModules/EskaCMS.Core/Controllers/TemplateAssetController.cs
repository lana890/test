using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Controllers
{

    [ApiController]
    [Route("api/TemplateAsset")]
    public class TemplateAssetController:ControllerBase
    {
        private readonly ITemplateAssetsServices _assetsTemplate;

        public TemplateAssetController(ITemplateAssetsServices assetsTemplate)
        {
            _assetsTemplate = assetsTemplate;
        }

        [HttpPost]
        [Route("UpsertAssetTemplate")]
        public async Task<IActionResult> UpsertAssetTemplate(Models.TemplateAssetsVM assetTemplate)
        {
            try
            {
                return Ok(await _assetsTemplate.UpsertTemplateAssets(assetTemplate));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpGet]
        [Route("GetTemplatesAssets")]
        public async Task<IActionResult> GetTemplatesAssets()
        {
            try
            {
                return Ok(await _assetsTemplate.GetTemplatesAssets());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        [HttpDelete]
        [Route("DeleteTemplateAssets/{Id}")]
        public async Task<IActionResult> DeleteTemplateAssets(long Id)
        {
            try
            {
                return Ok(await _assetsTemplate.ChangeStatus(Id, EStatus.Deleted));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        [HttpDelete]
        [Route("ChangeStatus/{Id}/{NewStatus}")]
        public async Task<IActionResult> DeleteTemplateAssets(long Id,EStatus NewStatus)
        {
            try
            {
                return Ok(await _assetsTemplate.ChangeStatus(Id, NewStatus));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
    }
}
