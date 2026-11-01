using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;

namespace EskaCMS.Infrastructure.Helpers
{
    /// <summary>
    /// Maps any exception onto the HTTP status code and the user-safe message that should be
    /// returned for it. This is the only place in the application that decides "what does this
    /// exception look like to the caller", so controllers, services and the global exception
    /// middleware all answer consistently.
    ///
    /// Recognised failures keep their meaning (validation detail, business rule text, "not
    /// found", "conflict", "service unavailable"); anything unrecognised is reported as a
    /// generic 500 and flagged <see cref="ApiErrorInfo.IsUnexpected"/> so the caller logs the
    /// full exception instead of showing it.
    /// </summary>
    public static class ExceptionMappingHelper
    {
        /// <summary>Status code used when the client itself cancelled the request.</summary>
        public const int ClientClosedRequestStatusCode = 499;

        /// <summary>
        /// Builds the status code, message and validation detail for <paramref name="exception"/>.
        /// Never returns null and never returns an empty message.
        /// </summary>
        public static ApiErrorInfo Map(Exception exception)
        {
            if (exception == null)
            {
                return new ApiErrorInfo(500, ErrorMessageHelper.GenericErrorMessage, null, true);
            }

            var error = Unwrap(exception);

            // ---- expected, explainable failures: the message is written for the user ----------

            if (error is BusinessRuleException businessRuleException)
            {
                return new ApiErrorInfo(
                    businessRuleException.StatusCode,
                    ErrorMessageHelper.Sanitize(businessRuleException.Message),
                    null,
                    false);
            }

            if (error is ValidationException validationException)
            {
                return FromValidationException(validationException);
            }

            // ---- authentication / authorization ----------------------------------------------

            if (error is UnauthorizedAccessException)
            {
                return new ApiErrorInfo(401, "You are not authorized to perform this action.", null, false);
            }

            // ---- not found -------------------------------------------------------------------

            if (error is KeyNotFoundException || error is FileNotFoundException || error is DirectoryNotFoundException)
            {
                return new ApiErrorInfo(404, "The requested resource was not found.", null, false);
            }

            // ---- bad input -------------------------------------------------------------------

            if (error is ArgumentException || error is FormatException || error is InvalidCastException
                || error is OverflowException || error is Newtonsoft.Json.JsonException
                || error is System.Text.Json.JsonException)
            {
                return new ApiErrorInfo(400, "The request contains invalid data. Please review the values and try again.", null, false);
            }

            // ---- the caller gave up ----------------------------------------------------------

            if (error is OperationCanceledException)
            {
                return new ApiErrorInfo(ClientClosedRequestStatusCode, "The request was cancelled.", null, false);
            }

            // ---- database --------------------------------------------------------------------

            if (error is DbUpdateConcurrencyException)
            {
                return new ApiErrorInfo(
                    409,
                    "This record was changed by someone else. Please reload it and try again.",
                    null,
                    false);
            }

            if (error is DbUpdateException dbUpdateException)
            {
                return FromDbUpdateException(dbUpdateException);
            }

            if (error is DbException)
            {
                return new ApiErrorInfo(500, ErrorMessageHelper.GenericDatabaseErrorMessage, null, true);
            }

            // ---- external services / integrations --------------------------------------------

            if (error is TimeoutException)
            {
                return new ApiErrorInfo(504, "The operation took too long to complete. Please try again.", null, false);
            }

            if (error is HttpRequestException || error is WebException || error is SocketException)
            {
                return new ApiErrorInfo(502, ErrorMessageHelper.GenericExternalServiceErrorMessage, null, true);
            }

            if (error is IOException)
            {
                return new ApiErrorInfo(500, "The file operation could not be completed. Please try again.", null, true);
            }

            // ---- not available ---------------------------------------------------------------

            if (error is NotImplementedException || error is NotSupportedException)
            {
                return new ApiErrorInfo(501, "This operation is not supported.", null, false);
            }

            // ---- anything else: null references, invalid state, unknown third party errors ----

            return new ApiErrorInfo(500, ErrorMessageHelper.GenericErrorMessage, null, true);
        }

        /// <summary>
        /// The user-safe message for <paramref name="exception"/> - the shortcut for call sites
        /// that only need text (a service returning an error string, a log-and-continue path).
        /// </summary>
        public static string GetSafeMessage(Exception exception)
        {
            return Map(exception).Message;
        }

        /// <summary>
        /// Unwraps the wrappers that hide the real failure: AggregateException from awaited work,
        /// TargetInvocationException from reflection calls.
        /// </summary>
        public static Exception Unwrap(Exception exception)
        {
            var current = exception;

            for (var depth = 0; depth < 10 && current != null; depth++)
            {
                if (current is AggregateException aggregateException)
                {
                    var flattened = aggregateException.Flatten();

                    var inner = flattened.InnerExceptions.FirstOrDefault();

                    if (inner == null)
                    {
                        return current;
                    }

                    current = inner;
                    continue;
                }

                if (current is TargetInvocationException targetInvocationException
                    && targetInvocationException.InnerException != null)
                {
                    current = targetInvocationException.InnerException;
                    continue;
                }

                return current;
            }

            return current ?? exception;
        }

        private static ApiErrorInfo FromValidationException(ValidationException validationException)
        {
            var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            var messages = new List<string>();

            if (validationException.ValidationResults != null)
            {
                foreach (var result in validationException.ValidationResults)
                {
                    if (result == null || string.IsNullOrWhiteSpace(result.ErrorMessage))
                    {
                        continue;
                    }

                    var message = ErrorMessageHelper.Sanitize(result.ErrorMessage);

                    if (string.IsNullOrWhiteSpace(message))
                    {
                        continue;
                    }

                    messages.Add(message);

                    var memberNames = result.MemberNames == null
                        ? new List<string>()
                        : result.MemberNames.Where(name => !string.IsNullOrWhiteSpace(name)).ToList();

                    if (memberNames.Count == 0)
                    {
                        memberNames.Add(string.Empty);
                    }

                    foreach (var memberName in memberNames)
                    {
                        if (errors.TryGetValue(memberName, out var existing))
                        {
                            errors[memberName] = existing.Concat(new[] { message }).Distinct().ToArray();
                        }
                        else
                        {
                            errors[memberName] = new[] { message };
                        }
                    }
                }
            }

            // The exception's own Message is prefixed with the target CLR type name, which is
            // internal detail - the validation messages themselves are what the user needs.
            var combined = messages.Count == 0
                ? "The request contains validation errors."
                : string.Join(" ", messages.Select(EnsureSentenceEnd));

            return new ApiErrorInfo(
                400,
                combined,
                errors.Count == 0 ? null : (IDictionary<string, string[]>)errors,
                false);
        }

        private static ApiErrorInfo FromDbUpdateException(DbUpdateException dbUpdateException)
        {
            // The provider message is never shown - it carries table, column and constraint
            // names - but it does tell us which friendly sentence fits.
            var providerMessage = ExceptionHelper.ThrowDeepException(dbUpdateException)?.Message ?? string.Empty;

            if (ContainsAny(providerMessage, "duplicate", "unique"))
            {
                return new ApiErrorInfo(409, "A record with the same values already exists.", null, false);
            }

            if (ContainsAny(providerMessage, "foreign key", "reference constraint"))
            {
                return new ApiErrorInfo(409, "This record is linked to other data and cannot be changed.", null, false);
            }

            if (ContainsAny(providerMessage, "cannot be null", "null value", "required"))
            {
                return new ApiErrorInfo(400, "Some required information is missing. Please review the values and try again.", null, false);
            }

            return new ApiErrorInfo(500, ErrorMessageHelper.GenericDatabaseErrorMessage, null, true);
        }

        private static bool ContainsAny(string value, params string[] markers)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (var index = 0; index < markers.Length; index++)
            {
                if (value.IndexOf(markers[index], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string EnsureSentenceEnd(string message)
        {
            var last = message[message.Length - 1];

            return last == '.' || last == '!' || last == '?' || last == ';'
                ? message
                : message + ".";
        }
    }
}
