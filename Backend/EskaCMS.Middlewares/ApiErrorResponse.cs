using System.Collections.Generic;

namespace EskaCMS.Infrastructure.Helpers
{
    /// <summary>
    /// The error body every API error is written as. One shape, whoever produces it - the
    /// global exception middleware, a controller catch block, the model validation factory - so
    /// a client only ever has to read <c>message</c> to show something meaningful.
    ///
    /// The property names are pinned for both serializers (Newtonsoft for MVC results,
    /// System.Text.Json for middleware writes) so the bytes on the wire are the same either way.
    /// Empty members are omitted rather than serialized as null, which is what used to make
    /// clients render "undefined".
    /// </summary>
    public sealed class ApiErrorResponse
    {
        /// <summary>The HTTP status code as a string, matching the API response envelope.</summary>
        [Newtonsoft.Json.JsonProperty("code", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("code")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string Code { get; set; }

        /// <summary>The readable, user-safe sentence to display. Always present.</summary>
        [Newtonsoft.Json.JsonProperty("message")]
        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// Same text as <see cref="Message"/>. Kept because the previous global handler answered
        /// with { "error": "..." } and clients already read that member.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("error", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("error")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string Error { get; set; }

        /// <summary>Per-field validation detail, omitted for non-validation errors.</summary>
        [Newtonsoft.Json.JsonProperty("errors", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("errors")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, string[]> Errors { get; set; }

        /// <summary>Correlation id, so a user-reported message can be found in the logs.</summary>
        [Newtonsoft.Json.JsonProperty("correlationId", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("correlationId")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string CorrelationId { get; set; }

        /// <summary>ASP.NET trace identifier for the request.</summary>
        [Newtonsoft.Json.JsonProperty("traceId", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("traceId")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string TraceId { get; set; }

        /// <summary>Builds the body for an already mapped error.</summary>
        public static ApiErrorResponse From(ApiErrorInfo error)
        {
            if (error == null)
            {
                return new ApiErrorResponse
                {
                    Code = "500",
                    Message = ErrorMessageHelper.GenericErrorMessage,
                    Error = ErrorMessageHelper.GenericErrorMessage
                };
            }

            return new ApiErrorResponse
            {
                Code = error.Code,
                Message = error.Message,
                Error = error.Message,
                Errors = error.Errors
            };
        }
    }
}
