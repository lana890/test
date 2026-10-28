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
using EskaCMS.Core.Cache;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Core.ViewModels;

namespace EskaCMS.Components.Controllers
{

    [ApiController]
    [Route("api/qr")]
    [Authorize]
    public class QRController : ControllerBase
    {
      
        private readonly IQrCodeService _QR_Service;
        // private readonly IWorkContext _workContext;

        public QRController(IQrCodeService QR_Service/* , IWorkContext workContext */)
        {
            _QR_Service = QR_Service;
            // _workContext = workContext;
        }


        [HttpPost]
        [Route("Generate")]
        public async Task<IActionResult> Generate(GenerateVM model)
        {
            try
            {
                return Ok(await _QR_Service.GenerateQrCode(model.Data, model.FileName));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        

    }
}