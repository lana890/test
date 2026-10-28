#nullable enable
using System;
using System.Collections.Generic;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Payments.Models.DirectPay
{
    /// <summary>The body of POST /api/DirectPay/CreatePayment.</summary>
    public class DirectPayCreatePaymentRequest
    {
        public CartTypes CartType { get; set; } = CartTypes.Shop;

        /// <summary>The active cart to pay (one order per cart). Null = the user's active cart.</summary>
        public long? CartId { get; set; }

        /// <summary>The page DirectPay's result returns the customer to, with ?OrderId= appended.</summary>
        public string? ReturnUrl { get; set; }

        /// <summary>"En" or "Ar": the language of DirectPay's page.</summary>
        public string? Language { get; set; }
    }

    /// <summary>What CreatePayment answers: the hosted page's address and its signed fields.</summary>
    public class DirectPayStartResult
    {
        public string Url { get; set; } = string.Empty;
        public SortedDictionary<string, string> Fields { get; set; } = new(StringComparer.Ordinal);
        public string TransactionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kept as JSON in the payment row's PaymentReceipt column: what the result needs to be
    /// checked against (amount, currency) and where to send the customer back. No secrets.
    /// </summary>
    public class DirectPayPaymentRecord
    {
        public string Gateway { get; set; } = "DirectPay";
        public string TransactionId { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string CurrencyIsoCode { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public long CultureId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// The SecureHash of the request the browser was given. The browser sees it, and DirectPay's
        /// hash covers values only (no names), so a response must never carry this same hash.
        /// </summary>
        public string? RequestHash { get; set; }

        public DirectPayResultRecord? Result { get; set; }
    }

    /// <summary>The result fields worth keeping (masked card number only, never card data).</summary>
    public class DirectPayResultRecord
    {
        public string? StatusCode { get; set; }
        public string? StatusDescription { get; set; }
        public string? GatewayName { get; set; }
        public string? GatewayStatusCode { get; set; }
        public string? GatewayStatusDescription { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Rrn { get; set; }
        public string? ApprovalCode { get; set; }
        public string? MaskedCardNumber { get; set; }
        public string? Amount { get; set; }
        public string? CurrencyIsoCode { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTimeOffset ReceivedAt { get; set; }
    }

    public enum DirectPayOutcomeKind
    {
        /// <summary>No order/payment matches the TransactionID.</summary>
        Unknown,
        /// <summary>The hash or the merchant id did not match: nothing was recorded.</summary>
        Rejected,
        /// <summary>The payment was already settled by an earlier response or notification.</summary>
        AlreadySettled,
        Paid,
        /// <summary>Not settled yet, or held for a person to review.</summary>
        Pending,
        Failed,
        /// <summary>Handling the result threw (logged); nothing may have been recorded.</summary>
        Error,
    }

    public class DirectPayOutcome
    {
        public DirectPayOutcomeKind Kind { get; set; }
        public long OrderId { get; set; }
        public string? ReturnUrl { get; set; }
    }

    /// <summary>
    /// A refusal. <see cref="Exception.Message"/> can be shown to the customer;
    /// <see cref="Detail"/> is for the log only.
    /// </summary>
    public class DirectPayException : Exception
    {
        public DirectPayException(string message, string? detail = null) : base(message)
        {
            Detail = detail;
        }

        public string? Detail { get; }
    }
}
