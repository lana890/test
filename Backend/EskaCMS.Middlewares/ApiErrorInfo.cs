using System.Collections.Generic;
using System.Globalization;

namespace EskaCMS.Infrastructure.Helpers
{
    /// <summary>
    /// The one shape every handled error is reduced to before it is written to a response:
    /// a status code, a readable message and - for validation failures - the per-field detail.
    /// Produced by <see cref="ExceptionMappingHelper"/>, consumed by the global exception
    /// middleware and by the controller error helpers.
    /// </summary>
    public sealed class ApiErrorInfo
    {
        public ApiErrorInfo()
        {
            StatusCode = 500;
            Message = ErrorMessageHelper.GenericErrorMessage;
        }

        public ApiErrorInfo(int statusCode, string message)
            : this(statusCode, message, null, false)
        {
        }

        public ApiErrorInfo(int statusCode, string message, IDictionary<string, string[]> errors, bool isUnexpected)
        {
            StatusCode = statusCode;

            Message = string.IsNullOrWhiteSpace(message)
                ? ErrorMessageHelper.ForStatusCode(statusCode)
                : message;

            Errors = errors;
            IsUnexpected = isUnexpected;
        }

        /// <summary>HTTP status code to answer with.</summary>
        public int StatusCode { get; set; }

        /// <summary>A readable, user-safe sentence. Never null and never empty.</summary>
        public string Message { get; set; }

        /// <summary>Per-field validation detail, or null when the error is not a validation failure.</summary>
        public IDictionary<string, string[]> Errors { get; set; }

        /// <summary>
        /// True when the exception was not recognised - the message shown is generic and the
        /// failure deserves an Error level log entry with the full exception.
        /// </summary>
        public bool IsUnexpected { get; set; }

        /// <summary>The status code as the string "code" used by the API response envelope.</summary>
        public string Code
        {
            get { return StatusCode.ToString(CultureInfo.InvariantCulture); }
        }
    }
}
