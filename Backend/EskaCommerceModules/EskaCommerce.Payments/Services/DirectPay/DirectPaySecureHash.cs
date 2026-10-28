#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace EskaCommerce.Payments.Services.DirectPay
{
    /// <summary>
    /// DirectPay's SecureHash (Integration Guide v2.1, Appendix B): SHA-256 of the merchant's
    /// Authentication Token followed by the value of every field, ordered by field name
    /// (ordinal), with no separators, written as lowercase hex. The hash field itself is left out.
    ///
    /// A few free-text fields are hashed form-URL-encoded (UTF-8, space as "+"), exactly as
    /// DirectPay's .NET sample does with HttpUtility.UrlEncode: PaymentDescription in a request,
    /// Response.StatusDescription and Response.GatewayStatusDescription in a response. Every other
    /// value is hashed as it is sent or received. The worked examples in Appendix B depend on this.
    /// </summary>
    public static class DirectPaySecureHash
    {
        public const string RequestHashField = "SecureHash";
        public const string ResponseHashField = "Response.SecureHash";

        private static readonly HashSet<string> EncodedFields = new(StringComparer.Ordinal)
        {
            "PaymentDescription",
            "Response.StatusDescription",
            "Response.GatewayStatusDescription",
        };

        /// <summary>
        /// The guide only says those fields are "UTF-8 encoded" into the hash. DirectPay's .NET sample
        /// encodes them with HttpUtility.UrlEncode (lowercase hex); a Java gateway's URLEncoder writes
        /// uppercase hex and also escapes ! ( ). The variants differ only for non-ASCII text and
        /// those few characters, which is what an Arabic description is made of, so a response is
        /// accepted when it matches any of them. Every variant still needs the token.
        /// </summary>
        private static readonly Func<string, string>[] ResponseEncoders = { DotNetEncode, UpperHexEncode, JavaEncode };

        /// <summary>The hash of a request's or a response's fields (any hash field is ignored).</summary>
        public static string Compute(string authenticationToken, IEnumerable<KeyValuePair<string, string>> fields)
        {
            return Hash(BuildInput(authenticationToken, fields, DotNetEncode));
        }

        /// <summary>
        /// The string that is hashed: the token, then each value in field-name order. Exposed so it
        /// can be compared with the "input to the Secure Hash generation routine" lines of Appendix B.
        /// </summary>
        public static string BuildInput(string authenticationToken, IEnumerable<KeyValuePair<string, string>> fields)
        {
            return BuildInput(authenticationToken, fields, DotNetEncode);
        }

        /// <summary>
        /// True when the response's own Response.SecureHash matches the hash of the other fields.
        /// Compared in constant time.
        /// </summary>
        public static bool Verify(string authenticationToken, IReadOnlyDictionary<string, string> responseFields)
        {
            if (string.IsNullOrEmpty(authenticationToken))
                return false;
            if (!responseFields.TryGetValue(ResponseHashField, out var received) || string.IsNullOrWhiteSpace(received))
                return false;

            var receivedBytes = Encoding.ASCII.GetBytes(received.Trim().ToLowerInvariant());
            var matched = false;
            foreach (var encode in ResponseEncoders)
            {
                var expected = Hash(BuildInput(authenticationToken, responseFields, encode));
                matched |= CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), receivedBytes);
            }
            return matched;
        }

        private static string BuildInput(string authenticationToken, IEnumerable<KeyValuePair<string, string>> fields, Func<string, string> encode)
        {
            var builder = new StringBuilder(authenticationToken ?? string.Empty);

            foreach (var field in fields
                         .Where(f => f.Key != RequestHashField && f.Key != ResponseHashField)
                         .OrderBy(f => f.Key, StringComparer.Ordinal))
            {
                var value = field.Value ?? string.Empty;
                builder.Append(EncodedFields.Contains(field.Key) ? encode(value) : value);
            }

            return builder.ToString();
        }

        private static string Hash(string input)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
        }

        /// <summary>HttpUtility.UrlEncode, as DirectPay's .NET sample: lowercase hex, - _ . ! * ( ) kept.</summary>
        private static string DotNetEncode(string value)
        {
            return HttpUtility.UrlEncode(value, Encoding.UTF8);
        }

        /// <summary>The same with uppercase hex.</summary>
        private static string UpperHexEncode(string value)
        {
            var encoded = DotNetEncode(value);
            var builder = new StringBuilder(encoded.Length);
            for (var i = 0; i < encoded.Length; i++)
            {
                if (encoded[i] == '%' && i + 2 < encoded.Length)
                {
                    builder.Append('%').Append(char.ToUpperInvariant(encoded[i + 1])).Append(char.ToUpperInvariant(encoded[i + 2]));
                    i += 2;
                }
                else
                {
                    builder.Append(encoded[i]);
                }
            }
            return builder.ToString();
        }

        /// <summary>java.net.URLEncoder with UTF-8: letters, digits and . - * _ kept, space as "+", uppercase hex.</summary>
        private static string JavaEncode(string value)
        {
            var builder = new StringBuilder(value.Length * 3);
            foreach (var b in Encoding.UTF8.GetBytes(value))
            {
                var c = (char)b;
                if (c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '.' or '-' or '*' or '_')
                    builder.Append(c);
                else if (c == ' ')
                    builder.Append('+');
                else
                    builder.Append('%').Append(b.ToString("X2"));
            }
            return builder.ToString();
        }
    }
}
