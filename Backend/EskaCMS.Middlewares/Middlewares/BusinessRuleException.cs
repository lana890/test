using System;

namespace EskaCMS.Infrastructure
{
    /// <summary>
    /// Thrown when a business rule stops an operation - "this order is already paid", "the
    /// coupon has expired". Its message is written for the end user, so the centralized error
    /// handling returns it as-is instead of replacing it with a generic sentence (every other
    /// unrecognised exception is treated as unexpected and hidden behind a generic message).
    ///
    /// Use it in services for expected, explainable failures; keep <see cref="ValidationException"/>
    /// for model validation and let genuinely unexpected failures bubble up untouched.
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message)
            : this(message, 400)
        {
        }

        public BusinessRuleException(string message, int statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public BusinessRuleException(string message, Exception innerException)
            : this(message, 400, innerException)
        {
        }

        public BusinessRuleException(string message, int statusCode, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        /// <summary>HTTP status code the rule maps to - 400 by default, 404/409/403 where it fits.</summary>
        public int StatusCode { get; }
    }
}
