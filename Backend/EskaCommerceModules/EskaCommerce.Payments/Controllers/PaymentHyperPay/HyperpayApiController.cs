using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Payments;

namespace EskaCommerce.Payments.Controllers.PaymentHyperPay
{
    [Area("PaymentHyperpay")]
    [Authorize]
    [Route("api/hyperpay")]
    public class StripeApiController : Controller
    {
        // private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;

        public StripeApiController(/* IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository */)
        {
            // _paymentProviderRepository = paymentProviderRepository;
        }

        //[HttpGet("config")]
        //public async Task<IActionResult> Config()
        //{
        //    var stripeProvider = await _paymentProviderRepository.Query().FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.HyperProviderId);
        //    var model = JsonConvert.DeserializeObject<MepsConfigForm>(stripeProvider.AdditionalSettings);
        //    return Ok(model);
        //}

        //[HttpPut("config")]
        //public async Task<IActionResult> Config([FromBody] MepsConfigForm model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var stripeProvider = await _paymentProviderRepository.Query().FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.HyperProviderId);
        //        stripeProvider.AdditionalSettings = JsonConvert.SerializeObject(model);
        //        await _paymentProviderRepository.SaveChangesAsync();
        //        return Accepted();
        //    }

        //    return BadRequest(ModelState);
        //}
    }
}
