using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Payments;

namespace EskaCommerce.Payments.Controllers
{
    [Area("Payments")]
    [Authorize]
    [Route("api/payments")]
    public class PaymentApiController : Controller
    {
        private readonly IRepository<Payment> _paymentRepository;
        private readonly ICurrencyService _currencyService;

        public PaymentApiController(IRepository<Payment> paymentRepository, ICurrencyService currencyService)
        {
            _paymentRepository = paymentRepository;
            _currencyService = currencyService;
        }

        [HttpGet("/api/orders/{orderId}/payments")]
        public async Task<IActionResult> GetByOrder(long orderId)
        {
            var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
            var DefaultCurrencyCulture = _currencyService.GetDefaultCurrencyCulture();
            var DefaultDecimalPlaces = _currencyService.GetDefaultDecimalPlaces(SiteId);


            var payments = await _paymentRepository.Query()
                .Where(x => x.OrderId == orderId)
                .Include("Order.Customer.CurrencyCulture")
                .Select(x => new
                {
                    x.Id,
                    x.Amount,
                    AmountString = x.Amount +" "+ x.Order.CurrencyCode,
                    x.PaymentFee,
                    PaymentFeeString =x.PaymentFee + " "+ x.Order.CurrencyCode,
                    x.OrderId,
                    x.GatewayTransactionId,
                    Status = x.Status.ToString(),
                    x.FailureMessage,
                    x.CreatedOn,
                    x.PaymentMethod
                }).ToListAsync();

            return Ok(payments);
        }
    }
}
