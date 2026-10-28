#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using EskaCommerce.Payments.Models.DirectPay;

namespace EskaCommerce.Payments.Services.DirectPay.Interfaces
{
    public interface IDirectPayService
    {
        /// <summary>
        /// Makes the order from the cart and answers DirectPay's hosted-page address with the
        /// signed Redirect Payment fields. <paramref name="requestHost"/> is the host the call came
        /// in on (a ReturnUrl on it is always accepted).
        /// </summary>
        Task<DirectPayStartResult> CreatePaymentAsync(DirectPayCreatePaymentRequest request, string requestHost);

        /// <summary>
        /// Checks and records a result DirectPay sent (the browser's form post or the
        /// server-to-server notification). <paramref name="source"/> is "response" or "notification".
        /// </summary>
        Task<DirectPayOutcome> HandleResultAsync(IReadOnlyDictionary<string, string> fields, string source);
    }
}
