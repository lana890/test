using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using static BusinessModels.BusinessModel;
using EskaCMS.Core.Data;

using Microsoft.AspNetCore.Authorization;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;

namespace EskaCMS.Components.Controllers
{

    [ApiController]
    [Route("api/Assets")]
    [Authorize]
    public class AssetsController : ControllerBase
    {
      
        private readonly IAssetsService _assetsService;

        public AssetsController(IAssetsService assetsService)
        {
            _assetsService=assetsService;
        }


        [HttpDelete]
        [Route("DeleteAsset/{AssetId}")]
        public async Task<IActionResult> DeleteAsset(long AssetId)
        {
            try
            {
                return Ok(await _assetsService.ChangeAssetsStatus(AssetId,EStatus.Deleted));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpDelete]
        [Route("ChangeAssetStatus/{AssetId}/{NewStatus}")]
        public async Task<IActionResult> ChangeAssetStatus(long AssetId,EStatus NewStatus)
        {
            try
            {
                return Ok(await _assetsService.ChangeAssetsStatus(AssetId, NewStatus));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("GetAssets")]
        public async Task<IActionResult> GetAssets([FromBody] GetAssertVM model )
        {
            try
            {

                return Ok(await _assetsService.GetAssets(model));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpGet]
        [Route("GetAssetsById")]
        public async Task<IActionResult> GetAssetsById([FromQuery] long Id)
        {
            try
            {

                return Ok(await _assetsService.GetAssetsById(Id));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("UpsertAsset")]
        public async Task<IActionResult> UpsertAsset(UpsertAssetVM Asset)
        {
            try
            {
                return Ok(await _assetsService.UpsertAssets(Asset));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        

    }
}