using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EskaCMS.Infrastructure.Helpers
{
    /// <summary>
    /// The single place where "some error" - an exception message, an API response body, a
    /// validation dictionary - is turned into ONE readable sentence that is safe to show to an
    /// end user.
    ///
    /// It is what stops a client from rendering "[object Object]", "undefined" or a raw JSON /
    /// stack-trace dump: whatever shape the error arrives in (plain string, { message }, { error },
    /// { errors }, ProblemDetails, ValidationProblemDetails, a serialized Exception, nested objects
    /// and arrays, or nothing at all) it always comes back as a non-empty human sentence.
    ///
    /// Used by the global exception middleware, by the ApiWrapper error envelope and by any
    /// service that reads an error body back from an external integration.
    /// </summary>
    public static class ErrorMessageHelper
    {
        /// <summary>Fallback shown when nothing meaningful (or nothing safe) could be extracted.</summary>
        public const string GenericErrorMessage = "An unexpected error occurred. Please try again later.";

        /// <summary>Shown instead of any database / provider level detail.</summary>
        public const string GenericDatabaseErrorMessage = "The operation could not be completed. Please try again later.";

        /// <summary>Shown instead of any external service / integration detail.</summary>
        public const string GenericExternalServiceErrorMessage = "A required service is temporarily unavailable. Please try again later.";

        private const int MaxMessageLength = 1000;
        private const int MaxDepth = 6;
        private const int MaxJoinedParts = 10;

        /// <summary>
        /// Property names that carry the human readable text, most specific first. Looked up
        /// case-insensitively so "message", "Message" and "MESSAGE" all match - a serialized
        /// .NET exception uses "Message", ProblemDetails uses "detail"/"title", most APIs use
        /// "message" or "error".
        /// </summary>
        private static readonly string[] MessageKeys =
        {
            "message",
            "errorMessage",
            "error_message",
            "error_description",
            "errorDescription",
            "detail",
            "title",
            "description",
            "reason",
            "statusText",
            "error"
        };

        /// <summary>
        /// Values that carry no information even though they are present. A body of literal
        /// "null" used to be shown to the user as the word "null".
        /// </summary>
        private static readonly string[] PlaceholderTokens =
        {
            "null",
            "undefined",
            "NaN",
            "{}",
            "[]",
            "-",
            "[object Object]"
        };

        /// <summary>Property names that hold a validation dictionary or a list of errors.</summary>
        private static readonly string[] ErrorsKeys =
        {
            "errors",
            "modelState",
            "validationErrors"
        };

        /// <summary>
        /// Markers that mean the text came from the database layer. Such text never reaches the
        /// client - it is replaced by <see cref="GenericDatabaseErrorMessage"/>.
        /// </summary>
        private static readonly string[] DatabaseMarkers =
        {
            "SqlException",
            "MySqlException",
            "MySqlConnector",
            "Npgsql",
            "OracleException",
            "ORA-",
            "SQLSTATE",
            "DbUpdateException",
            "DbUpdateConcurrencyException",
            "while saving the entity changes",
            "See the inner exception for details",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.Data.SqlClient",
            "System.Data.SqlClient",
            "Duplicate entry",
            "duplicate key",
            "PRIMARY KEY constraint",
            "FOREIGN KEY constraint",
            "UNIQUE KEY constraint",
            "UNIQUE constraint",
            "Invalid object name",
            "Invalid column name",
            "Incorrect syntax near",
            "statement conflicted with the",
            "String or binary data would be truncated",
            "Timeout expired",
            "Data Source=",
            "Initial Catalog=",
            "Integrated Security=",
            "User ID=",
            "Password=",
            "connection string",
            "deadlocked on lock resources"
        };

        /// <summary>
        /// Markers that mean the text exposes internal implementation detail (stack frames,
        /// source paths, type names). Such text is replaced by <see cref="GenericErrorMessage"/>.
        /// </summary>
        private static readonly string[] InternalMarkers =
        {
            "StackTrace",
            "{\"",
            ":line ",
            ".cs:",
            "at EskaCMS.",
            "at System.",
            "at Microsoft.",
            "at lambda_method",
            "--- End of stack trace",
            "Exception of type '",
            "Object reference not set to an instance",
            "EskaDCMSDbContext",
            "C:\\",
            "D:\\",
            "/app/",
            "/src/"
        };

        /// <summary>
        /// Turns an API / integration response body into a readable message. Returns a generic
        /// sentence (never null, never empty) when the body carries nothing usable.
        /// </summary>
        public static string FromResponseBody(string body)
        {
            return FromResponseBody(body, 0);
        }

        /// <summary>
        /// Turns an API / integration response body into a readable message, falling back to the
        /// standard sentence for <paramref name="statusCode"/> when the body carries nothing usable.
        /// </summary>
        public static string FromResponseBody(string body, int statusCode)
        {
            var extracted = TryExtractMessage(body);

            return string.IsNullOrWhiteSpace(extracted)
                ? ForStatusCode(statusCode)
                : extracted;
        }

        /// <summary>
        /// Same as <see cref="FromResponseBody(string, int)"/> but returns null - instead of a
        /// generic sentence - when the body carries no readable message. Use it when the caller
        /// wants to supply its own fallback.
        /// </summary>
        public static string TryExtractMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            var trimmed = body.Trim();

            // An HTML error page (a reverse proxy page, the developer exception page) has no
            // message to show - anything we pulled out of it would be markup.
            if (trimmed.StartsWith("<", StringComparison.Ordinal))
            {
                return null;
            }

            if (LooksLikeJson(trimmed))
            {
                try
                {
                    return Sanitize(ExtractFromToken(JToken.Parse(trimmed), 0));
                }
                catch (JsonException)
                {
                    // Not valid JSON after all - fall through and treat it as plain text.
                }
                catch (Exception)
                {
                    // Never let message extraction itself fail a response.
                }
            }

            return Sanitize(trimmed);
        }

        /// <summary>
        /// Removes anything that must not reach an end user (stack frames, database and
        /// connection detail, source paths) and normalises whitespace. Returns null when nothing
        /// readable is left, so callers can apply their own fallback.
        /// </summary>
        public static string Sanitize(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            var value = message.Trim();

            if (value.StartsWith("<", StringComparison.Ordinal))
            {
                return null;
            }

            // Cut the stack trace off: ".NET" renders every frame as "   at Some.Method()".
            var frameIndex = value.IndexOf("   at ", StringComparison.Ordinal);

            if (frameIndex == 0)
            {
                return null;
            }

            if (frameIndex > 0)
            {
                value = value.Substring(0, frameIndex);
            }

            value = CollapseWhitespace(value).Trim(' ', '"', '\'');

            if (value.Length == 0 || IsPlaceholderToken(value))
            {
                return null;
            }

            if (ContainsAny(value, DatabaseMarkers))
            {
                return GenericDatabaseErrorMessage;
            }

            if (ContainsAny(value, InternalMarkers))
            {
                return GenericErrorMessage;
            }

            if (value.Length > MaxMessageLength)
            {
                value = value.Substring(0, MaxMessageLength).TrimEnd() + "...";
            }

            return value;
        }

        /// <summary>
        /// True when the text exposes database or internal implementation detail and must not be
        /// shown to an end user.
        /// </summary>
        public static bool ContainsSensitiveDetails(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            return ContainsAny(message, DatabaseMarkers) || ContainsAny(message, InternalMarkers);
        }

        /// <summary>The standard sentence for an HTTP status code.</summary>
        public static string ForStatusCode(int statusCode)
        {
            switch (statusCode)
            {
                case 400: return "Bad request.";
                case 401: return "Unauthorized access.";
                case 402: return "Payment required.";
                case 403: return "Forbidden access.";
                case 404: return "Resource not found.";
                case 405: return "Method not allowed.";
                case 406: return "Not acceptable.";
                case 407: return "Proxy authentication required.";
                case 408: return "Request timeout.";
                case 409: return "Conflict.";
                case 410: return "Resource is gone.";
                case 411: return "Length is required.";
                case 413: return "The uploaded content is too large.";
                case 415: return "Unsupported media type.";
                case 422: return "The request contains validation errors.";
                case 429: return "Too many requests. Please try again later.";

                case 500: return "Internal server error.";
                case 501: return "Not implemented.";
                case 502: return "Bad gateway.";
                case 503: return "Service unavailable.";
                case 504: return "Gateway timeout.";
                case 505: return "HTTP version not supported.";
            }

            if (statusCode >= 400 && statusCode < 500)
            {
                return "A client error occurred.";
            }

            if (statusCode >= 500)
            {
                return "A server error occurred.";
            }

            return GenericErrorMessage;
        }

        /// <summary>
        /// Text that is technically present but says nothing - a literal "null"/"undefined"
        /// body, an empty object, or a message a client already mangled into "[object Object]".
        /// Treated as "no message" so the status code's sentence is used instead.
        /// </summary>
        private static bool IsPlaceholderToken(string value)
        {
            for (var index = 0; index < PlaceholderTokens.Length; index++)
            {
                if (string.Equals(value, PlaceholderTokens[index], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeJson(string trimmed)
        {
            if (trimmed.Length < 2)
            {
                return false;
            }

            var first = trimmed[0];
            var last = trimmed[trimmed.Length - 1];

            return (first == '{' && last == '}')
                || (first == '[' && last == ']')
                || (first == '"' && last == '"');
        }

        private static string ExtractFromToken(JToken token, int depth)
        {
            if (token == null || depth > MaxDepth)
            {
                return null;
            }

            switch (token.Type)
            {
                case JTokenType.String:
                    return token.Value<string>();

                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.Boolean:
                case JTokenType.Date:
                case JTokenType.Guid:
                case JTokenType.Uri:
                    return token.ToString();

                case JTokenType.Array:
                    return JoinParts(((JArray)token).Select(item => ExtractFromToken(item, depth + 1)));

                case JTokenType.Object:
                    return ExtractFromObject((JObject)token, depth);

                default:
                    return null;
            }
        }

        private static string ExtractFromObject(JObject obj, int depth)
        {
            // Validation shapes first - ValidationProblemDetails and ModelState both put the
            // detail the user needs under "errors", with the generic title on top.
            foreach (var errorsKey in ErrorsKeys)
            {
                var errorsToken = obj.GetValue(errorsKey, StringComparison.OrdinalIgnoreCase);

                var fromErrors = ExtractFromErrors(errorsToken, depth + 1);

                if (!string.IsNullOrWhiteSpace(fromErrors))
                {
                    return fromErrors;
                }
            }

            foreach (var key in MessageKeys)
            {
                var value = obj.GetValue(key, StringComparison.OrdinalIgnoreCase);

                if (value == null)
                {
                    continue;
                }

                var extracted = ExtractFromToken(value, depth + 1);

                if (!string.IsNullOrWhiteSpace(extracted))
                {
                    return extracted;
                }
            }

            // A nested wrapper such as { "result": { "message": "..." } } or our own
            // { "content": { "error": "..." } } envelope.
            foreach (var property in obj.Properties())
            {
                if (property.Value == null
                    || (property.Value.Type != JTokenType.Object && property.Value.Type != JTokenType.Array))
                {
                    continue;
                }

                var extracted = ExtractFromToken(property.Value, depth + 1);

                if (!string.IsNullOrWhiteSpace(extracted))
                {
                    return extracted;
                }
            }

            return null;
        }

        /// <summary>
        /// Flattens a validation container: { "Name": ["is required"], "Age": ["is invalid"] },
        /// a plain array of messages, or a single string.
        /// </summary>
        private static string ExtractFromErrors(JToken errorsToken, int depth)
        {
            if (errorsToken == null || depth > MaxDepth)
            {
                return null;
            }

            if (errorsToken.Type == JTokenType.Object)
            {
                var parts = ((JObject)errorsToken)
                    .Properties()
                    .Select(property => ExtractFromToken(property.Value, depth + 1));

                return JoinParts(parts);
            }

            return ExtractFromToken(errorsToken, depth);
        }

        private static string JoinParts(IEnumerable<string> parts)
        {
            var usable = parts
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxJoinedParts)
                .ToList();

            return usable.Count == 0 ? null : string.Join(" ", usable.Select(EnsureSentenceEnd)).Trim();
        }

        private static string EnsureSentenceEnd(string part)
        {
            var last = part[part.Length - 1];

            return last == '.' || last == '!' || last == '?' || last == ';'
                ? part
                : part + ".";
        }

        private static string CollapseWhitespace(string value)
        {
            var builder = new StringBuilder(value.Length);
            var previousWasWhitespace = false;

            foreach (var character in value)
            {
                if (char.IsWhiteSpace(character))
                {
                    if (!previousWasWhitespace)
                    {
                        builder.Append(' ');
                    }

                    previousWasWhitespace = true;
                }
                else
                {
                    builder.Append(character);
                    previousWasWhitespace = false;
                }
            }

            return builder.ToString();
        }

        private static bool ContainsAny(string value, string[] markers)
        {
            for (var index = 0; index < markers.Length; index++)
            {
                if (value.IndexOf(markers[index], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
