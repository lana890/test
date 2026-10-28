#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Integrations.Helpers;
using EskaCommerce.Payments.Models.DirectPay;
using EskaCommerce.Payments.Services.DirectPay;
using EskaCommerce.Payments.Services.DirectPay.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EskaCommerce.Payments.Controllers.DirectPay
{
    /// <summary>
    /// DirectPay (formerly SmartRoute) Redirect Payment.
    ///
    ///  POST api/DirectPay/CreatePayment  (signed in)   makes the order, answers {url, fields, transactionId}
    ///  POST api/DirectPay/Response       (anonymous)   DirectPay's ResponseBackURL: the customer's browser
    ///                                                 posts the result; we record it and redirect to
    ///                                                 ReturnUrl?OrderId=&lt;encrypted order id&gt;
    ///  POST api/DirectPay/Notification   (anonymous)   DirectPay's Update Notification, server to server
    ///
    /// Response and Notification are trusted only through DirectPay's SecureHash. Both paths must be
    /// in CypherSecurity:ExecludedApis, since DirectPay cannot send the site's signature headers.
    /// </summary>
    [ApiController]
    [Route("api/DirectPay")]
    public class DirectPayController : ControllerBase
    {
        private const string ResponseFieldPrefix = "Response.";
        private const int MaxResultBodyLength = 64 * 1024;

        private readonly IDirectPayService _directPay;
        private readonly IWorkContext _workContext;
        private readonly IdcmsLogService _log;

        public DirectPayController(IDirectPayService directPay, IWorkContext workContext, IdcmsLogService log)
        {
            _directPay = directPay;
            _workContext = workContext;
            _log = log;
        }

        [HttpPost("CreatePayment")]
        [Authorize]
        public async Task<IActionResult> CreatePayment([FromBody] DirectPayCreatePaymentRequest request)
        {
            long userId = 0;
            SemaphoreSlim? userLock = null;
            var locked = false;
            try
            {
                // One start at a time per user: a double click must not make two orders.
                userId = await _workContext.GetCurrentUserId();
                userLock = UserPaymentLockHelper.GetLock(userId);
                await userLock.WaitAsync();
                locked = true;

                var result = await _directPay.CreatePaymentAsync(request ?? new DirectPayCreatePaymentRequest(), Request.Host.Host);

                // A JObject keeps DirectPay's field names exactly as written (MerchantID, ResponseBackURL ...):
                // neither MVC's camelCase resolver nor the ApiWrapper renames JToken keys.
                var fields = new JObject();
                foreach (var field in result.Fields)
                    fields[field.Key] = field.Value;

                return Ok(new JObject
                {
                    ["url"] = result.Url,
                    ["fields"] = fields,
                    ["transactionId"] = result.TransactionId,
                });
            }
            catch (DirectPayException ex)
            {
                await _log.AddError("DirectPay.CreatePayment", ex.Detail ?? ex.Message, ex.Message, null, SiteIdOrNull());
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                await _log.AddError("DirectPay.CreatePayment", ex.ToString(), ex.GetBaseException().Message, null, SiteIdOrNull());
                return BadRequest(DirectPayService.GenericStartError);
            }
            finally
            {
                if (locked)
                    userLock!.Release();
                if (userId != 0)
                    UserPaymentLockHelper.ReleaseLock(userId);
            }
        }

        /// <summary>
        /// DirectPay's ResponseBackURL. The customer's browser arrives here with the result (a form
        /// post; a query string is accepted too). Whatever happens, the customer is sent back to the
        /// journey page with ?OrderId=, where the page reads the order's real status, and the result of
        /// the work after the payment (a server flow's "Read the result", which waits for it there).
        /// </summary>
        [AllowAnonymous]
        [AcceptVerbs("GET", "POST")]
        [Route("Response")]
        public async Task<IActionResult> ResponseBack()
        {
            DirectPayOutcome outcome;
            try
            {
                var fields = await ReadResultFieldsAsync();
                outcome = await _directPay.HandleResultAsync(fields, "response");
            }
            catch (Exception ex)
            {
                await _log.AddError("DirectPay.response", ex.ToString(), "Could not read the result", null, null);
                return Redirect("/");
            }

            if (outcome.OrderId > 0 && !string.IsNullOrWhiteSpace(outcome.ReturnUrl))
            {
                var encryptedOrderId = HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(outcome.OrderId.ToString(CultureInfo.InvariantCulture)));
                var separator = outcome.ReturnUrl.Contains('?') ? "&" : "?";
                return Redirect($"{outcome.ReturnUrl}{separator}OrderId={encryptedOrderId}");
            }

            // Nothing matched this transaction: no address of ours to return to.
            return Redirect("/");
        }

        /// <summary>
        /// DirectPay's Update Notification (server to server). 200 once handled, also when the
        /// message is refused (sending it again would not change that); 500 only when handling
        /// failed, so a retry can succeed.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("Notification")]
        public async Task<IActionResult> Notification()
        {
            try
            {
                var fields = await ReadResultFieldsAsync();
                var outcome = await _directPay.HandleResultAsync(fields, "notification");
                return outcome.Kind == DirectPayOutcomeKind.Error ? StatusCode(500) : Ok();
            }
            catch (Exception ex)
            {
                await _log.AddError("DirectPay.notification", ex.ToString(), "Could not read the result", null, null);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// The Response.* fields DirectPay sent: a form post (the browser, and normally the
        /// notification), else a body of name=value pairs or JSON, else the query string. Only
        /// Response.* names are kept: they are what the SecureHash covers (the ResponseBackURL's own
        /// ?ngsw-bypass=1 is not one of them).
        /// </summary>
        private async Task<IReadOnlyDictionary<string, string>> ReadResultFieldsAsync()
        {
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);

            void Add(string key, string? value)
            {
                if (key.StartsWith(ResponseFieldPrefix, StringComparison.Ordinal))
                    fields[key] = value ?? string.Empty;
            }

            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                foreach (var item in form)
                    Add(item.Key, item.Value.ToString());
            }
            else if (HttpMethods.IsPost(Request.Method))
            {
                var body = await ReadBodyAsync();
                var trimmed = body.Trim();
                if (trimmed.StartsWith('{'))
                {
                    try
                    {
                        foreach (var property in JObject.Parse(trimmed).Properties())
                            Add(property.Name, property.Value.Type == JTokenType.Null ? string.Empty : property.Value.ToString());
                    }
                    catch (JsonException)
                    {
                        // Not JSON after all: nothing to read.
                    }
                }
                else if (trimmed.Length > 0)
                {
                    foreach (var item in QueryHelpers.ParseQuery(trimmed))
                        Add(item.Key, item.Value.ToString());
                }
            }

            if (fields.Count == 0)
            {
                foreach (var item in Request.Query)
                    Add(item.Key, item.Value.ToString());
            }

            return fields;
        }

        private async Task<string> ReadBodyAsync()
        {
            Request.EnableBuffering();
            if (Request.Body.CanSeek)
                Request.Body.Position = 0;

            var buffer = new char[MaxResultBodyLength];
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
            var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);

            if (Request.Body.CanSeek)
                Request.Body.Position = 0;
            return new string(buffer, 0, read);
        }

        private long? SiteIdOrNull()
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                return siteId > 0 ? siteId : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
