#nullable enable
using Newtonsoft.Json;

namespace EskaCommerce.Payments.Models.DirectPay
{
    /// <summary>
    /// DirectPay's settings for a site. Everything except the Authentication Token comes from the
    /// provider row's AdditionalSettings (table Payments_PaymentProvider, Id 'DirectPay'), read with
    /// Newtonsoft, so the JSON property names are case-insensitive.
    /// </summary>
    public class DirectPaySettings
    {
        /// <summary>The merchant's id at DirectPay (MerchantID).</summary>
        public string? MerchantId { get; set; }

        /// <summary>
        /// The merchant's Authentication Token. It signs every message, so anyone who holds it can
        /// forge a "paid" result. It is read from the server configuration
        /// (<c>DirectPay:AuthenticationToken</c>), never from the provider row: any signed-in
        /// visitor, guests included, can switch a provider on and read its AdditionalSettings through
        /// <c>api/payments-providers</c>.
        /// </summary>
        [JsonIgnore]
        public string? AuthenticationToken { get; set; }

        /// <summary>DirectPay's Redirect Payment address (…/SmartRoutePaymentWeb/SRPayMsgHandler).</summary>
        public string? PaymentUrl { get; set; }

        /// <summary>
        /// Where DirectPay posts the result from the customer's browser: this site's
        /// /api/DirectPay/Response, ending with ?ngsw-bypass=1 so the site's service worker does
        /// not answer the POST navigation itself.
        /// </summary>
        public string? ResponseBackUrl { get; set; }

        /// <summary>Optional theme of DirectPay's hosted page.</summary>
        public string? ThemeId { get; set; }

        /// <summary>Message version; 2.0 makes the response say which method was used.</summary>
        public string? Version { get; set; }

        /// <summary>0 = web.</summary>
        public string? Channel { get; set; }

        /// <summary>
        /// Hosts a ReturnUrl may point to, comma separated. The host the request came in on is
        /// always allowed; anything else is refused (an open redirect otherwise).
        /// </summary>
        public string? AllowedReturnHosts { get; set; }
    }
}
