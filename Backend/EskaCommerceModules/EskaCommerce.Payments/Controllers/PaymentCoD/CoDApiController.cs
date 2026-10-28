using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity.Payments;

using EskaCommerce.Payments.Models.PaymentCoD;
using EskaCMS.Entities.Entity;
namespace EskaCommerce.Payments.Controllers.PaymentCoD
{
    [Authorize]
    [Area("PaymentCoD")]
    [Route("api/cod")]
    public class CoDApiController : Controller
    {
          private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;

      

        public CoDApiController(IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository)
        {
            _paymentProviderRepository = paymentProviderRepository;
        }

        [HttpGet("config")]
        public async Task<IActionResult> Config()
        {
            var codProvider = await _paymentProviderRepository.Query().FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.CODProviderId);
            if (string.IsNullOrEmpty(codProvider.AdditionalSettings))
            {
                return Ok(new CoDSetting());
            }

            var model = JsonConvert.DeserializeObject<CoDSetting>(codProvider.AdditionalSettings);
            return Ok(model);
        }

        [HttpPut("config")]
        public async Task<IActionResult> Config([FromBody] CoDSetting model)
        {
            if (ModelState.IsValid)
            {
                var codProvider = await _paymentProviderRepository.Query().FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.CODProviderId);
                codProvider.AdditionalSettings = JsonConvert.SerializeObject(model);
                await _paymentProviderRepository.SaveChangesAsync();
                return Accepted();
            }

            return BadRequest(ModelState);
        }
    }
}
