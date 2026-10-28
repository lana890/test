using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Payments;

namespace EskaCommerce.Payments.Controllers
{
    [Area("Payments")]
    [Authorize]
    [Route("api/payments-providers")]
    public class PaymentProviderApiController : Controller
    {
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
     

        public PaymentProviderApiController(IRepositoryWithTypedId<PaymentProvider,
            string> paymentProviderRepositor
         )
        {
            _paymentProviderRepository = paymentProviderRepositor;
            
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var SiteId = long.Parse(Request.Headers["siteId"].ToString());
            var providers = await _paymentProviderRepository.Query()
                .Where(x => x.SiteId == SiteId && x.IsEnabled)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.SiteId,
                    x.AdditionalSettings,
                    x.IsEnabled
                }).ToListAsync();

            return Json(providers);
        }

        [HttpPost("{id}/enable")]
        public async Task<IActionResult> Enable(string id)
        {
            var SiteId = long.Parse(Request.Headers["siteId"].ToString());

            var provider = await _paymentProviderRepository.Query().Where(x=>x.SiteId==SiteId && x.Id == id).FirstOrDefaultAsync();
            provider.IsEnabled = true;
            await _paymentProviderRepository.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id}/disable")]
        public async Task<IActionResult> Disable(string id)
        {
            var SiteId = long.Parse(Request.Headers["siteId"].ToString());
            var provider = await _paymentProviderRepository.Query().Where(x => x.SiteId == SiteId && x.Id == id).FirstOrDefaultAsync();
            provider.IsEnabled = false;
            await _paymentProviderRepository.SaveChangesAsync();
            return NoContent();
        }
    }
}
