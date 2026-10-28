using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Definition
{
    /// <summary>A public key another system signs its webhook calls with. The flow keeps only this: nothing secret.</summary>
    public sealed class WebhookPublicKey
    {
        /// <summary>What the admin calls it, e.g. "Finance production".</summary>
        public string Name { get; init; } = "";

        /// <summary>The key as DER SubjectPublicKeyInfo.</summary>
        public byte[] Spki { get; init; } = Array.Empty<byte>();

        /// <summary>"ECDSA P-256" or "RSA 2048" (3072, 4096).</summary>
        public string Type { get; init; } = "";

        /// <summary>SHA-256 of the key (Spki), 64 lower-case hex digits: how both sides tell keys apart.</summary>
        public string Fingerprint { get; init; } = "";

        /// <summary>When it was added (yyyy-MM-dd), for the Admin.</summary>
        public string? AddedOn { get; init; }

        /// <summary>The fingerprint's first 16 digits, as people compare it ("3f5a 91c0 7d2e 4b18").</summary>
        public string ShortFingerprint => WebhookAuth.Short(Fingerprint);
    }

    /// <summary>A public key read from what an admin pasted (PEM, a certificate, a JWK or base64), or why it was refused.</summary>
    public sealed record ParsedPublicKey(byte[] Spki, string Type, string Fingerprint)
    {
        /// <summary>The key as the flow writes it: base64 of the DER SubjectPublicKeyInfo, on one line.</summary>
        public string Base64 => Convert.ToBase64String(Spki);
    }

    /// <summary>An address or a range of them (CIDR) that may call a webhook flow, and how it was written.</summary>
    public readonly record struct AddressRule(IPNetwork Network, string Text)
    {
        public bool Contains(IPAddress address) => Network.Contains(WebhookAuth.Normalize(address)!);
    }

    /// <summary>
    /// How another system proves that a call to a webhook flow is its own, as pure functions (the
    /// tests run them without a server).
    ///
    /// Signed requests (trigger.auth "signature"): the system signs every request with its PRIVATE
    /// key; the flow keeps only the matching PUBLIC key(s). Nothing secret is sent, and nothing secret
    /// is stored on the server: a copy of the flow, the database or the logs cannot forge a call. A
    /// request cannot be changed (the body's hash is signed) and cannot be used again later (its time
    /// is signed and the server takes it only within MaxAgeSeconds of its own clock).
    ///
    /// What is signed, UTF-8, lines joined with "\n" (no line break at the end):
    ///   DCMS-FLOW-SIGNATURE-V1
    ///   {X-Flow-Timestamp: Unix time in seconds}
    ///   {the method: POST or GET}
    ///   {the path from /api/ on, without the query: /api/ServerFlows/hook/6/sadad-paid}
    ///   {the SHA-256 of the body's bytes exactly as sent, 64 lower-case hex digits (GET: of nothing)}
    /// with ECDSA P-256 + SHA-256 (the signature as DER or as r|s, 64 bytes) or RSA (2048 bits or
    /// more) + SHA-256 (PKCS#1 v1.5 or PSS), sent base64 in X-Flow-Signature.
    ///
    /// The key in a header (trigger.auth "key", the older way) stays for tests and while a system
    /// moves over: a signed flow may still take its old key until a date (keyUntil).
    /// </summary>
    public static class WebhookAuth
    {
        public const string KeyMode = "key";
        public const string SignatureMode = "signature";

        public const string Version = "DCMS-FLOW-SIGNATURE-V1";
        public const string TimestampHeader = "X-Flow-Timestamp";
        public const string SignatureHeader = "X-Flow-Signature";

        /// <summary>How far a request's time may be from the server's, either way, by default (ServerFlows:SignatureMaxAgeSeconds).</summary>
        public const int DefaultMaxAgeSeconds = 300;
        public const int MinMaxAgeSeconds = 30;
        public const int MaxMaxAgeSeconds = 900;

        /// <summary>Public keys one flow may hold at the same time (the current one, and its replacement while it is rolled out).</summary>
        public const int MaxPublicKeys = 3;

        /// <summary>Addresses or ranges one flow may list.</summary>
        public const int MaxAddressRules = 50;

        /// <summary>How long a flow may keep taking its old key once it takes signed requests.</summary>
        public const int MaxKeyDays = 90;

        public const int MinRsaBits = 2048;
        public const int MaxRsaBits = 8192;

        /// <summary>
        /// Cloudflare's published addresses (cloudflare.com/ips, October 2026). A call that reaches the
        /// server from one of them came through Cloudflare, and the caller's own address is in the
        /// CF-Connecting-IP header, which Cloudflare sets itself. ServerFlows:CloudflareRanges replaces
        /// the list (an empty list: never read the header).
        /// </summary>
        public static readonly IReadOnlyList<string> CloudflareRanges = new[]
        {
            "173.245.48.0/20", "103.21.244.0/22", "103.22.200.0/22", "103.31.4.0/22", "141.101.64.0/18",
            "108.162.192.0/18", "190.93.240.0/20", "188.114.96.0/20", "197.234.240.0/22", "198.41.128.0/17",
            "162.158.0.0/15", "104.16.0.0/13", "104.24.0.0/14", "172.64.0.0/13", "131.0.72.0/22",
            "2400:cb00::/32", "2606:4700::/32", "2803:f800::/32", "2405:b500::/32", "2405:8100::/32",
            "2a06:98c0::/29", "2c0f:f248::/32",
        };

        private static readonly JsonSerializerSettings JsonRead = new() { DateParseHandling = DateParseHandling.None, MaxDepth = 16 };

        #region What is signed

        /// <summary>The path a caller signs: from /api/ on, as the flow's address and the run's address are built.</summary>
        public static string PathOf(long siteId, string flowName, long? runId = null) =>
            runId == null
                ? string.Create(CultureInfo.InvariantCulture, $"/api/ServerFlows/hook/{siteId}/{flowName}")
                : string.Create(CultureInfo.InvariantCulture, $"/api/ServerFlows/hook/{siteId}/{flowName}/run/{runId}");

        /// <summary>The text a caller signs for a request (see the class).</summary>
        public static string StringToSign(string timestamp, string method, string path, byte[] body) =>
            string.Join("\n", Version, timestamp.Trim(), method.Trim().ToUpperInvariant(), path, Hex(SHA256.HashData(body ?? Array.Empty<byte>())));

        /// <summary>
        /// A request's identity for "the same request again": the hash of what it signs. A request
        /// sent twice (a retry, or a copy someone replays within the time allowed) is the same
        /// request; its signature is not used, as another valid signature of the same text exists.
        /// </summary>
        public static string RequestId(string stringToSign) => Hex(SHA256.HashData(Encoding.UTF8.GetBytes(stringToSign)))[..32];

        #endregion

        #region The request's time

        public enum TimeCheck { Ok, Missing, NotANumber, TooFar }

        /// <summary>Whether X-Flow-Timestamp is a Unix time within maxAgeSeconds of the server's clock, and how far it is (seconds, + = ahead).</summary>
        public static TimeCheck CheckTime(string? header, DateTimeOffset now, int maxAgeSeconds, out long offBySeconds)
        {
            offBySeconds = 0;
            var text = (header ?? "").Trim();
            if (text.Length == 0)
                return TimeCheck.Missing;
            if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
                return TimeCheck.NotANumber;
            offBySeconds = seconds - now.ToUnixTimeSeconds();
            return Math.Abs(offBySeconds) <= ClampMaxAge(maxAgeSeconds) ? TimeCheck.Ok : TimeCheck.TooFar;
        }

        public static int ClampMaxAge(int seconds) => seconds <= 0 ? DefaultMaxAgeSeconds : Math.Clamp(seconds, MinMaxAgeSeconds, MaxMaxAgeSeconds);

        /// <summary>Whether a time is in milliseconds (12 digits or more), the usual mistake (JavaScript's Date.now()).</summary>
        public static bool LooksLikeMilliseconds(string? header) =>
            long.TryParse((header ?? "").Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= 100_000_000_000L;

        #endregion

        #region Signatures

        /// <summary>The signature header's bytes (base64 or base64url, with or without padding); null when it is not base64.</summary>
        public static byte[]? SignatureBytes(string? header)
        {
            var text = (header ?? "").Trim();
            if (text.Length == 0 || text.Length > 4096)
                return null;
            text = text.Replace('-', '+').Replace('_', '/');
            text = text.PadRight(text.Length + (4 - text.Length % 4) % 4, '=');
            try
            {
                return Convert.FromBase64String(text);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        /// <summary>The key, of those given, that signed this text; null when none did.</summary>
        public static WebhookPublicKey? SignedBy(IEnumerable<WebhookPublicKey> keys, string stringToSign, string? signatureHeader)
        {
            var signature = SignatureBytes(signatureHeader);
            if (signature == null)
                return null;
            var data = Encoding.UTF8.GetBytes(stringToSign);
            return keys.FirstOrDefault(k => Verify(k.Spki, data, signature));
        }

        /// <summary>Whether <paramref name="signature"/> is <paramref name="data"/> signed with the private key of this public key (SHA-256).</summary>
        public static bool Verify(byte[] spki, byte[] data, byte[] signature)
        {
            if (TryEcdsa(spki) is { } ec)
            {
                using (ec)
                    return Try(() => ec.VerifyData(data, signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                        || Try(() => ec.VerifyData(data, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));
            }
            if (TryRsa(spki) is { } rsa)
            {
                using (rsa)
                    return Try(() => rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                        || Try(() => rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
            }
            return false;
        }

        private static bool Try(Func<bool> verify)
        {
            try
            {
                return verify();
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        #endregion

        #region Keys

        /// <summary>
        /// A new key pair for another system: ECDSA P-256. The private key is returned once (PEM,
        /// PKCS#8, and as a JWK for platforms without PEM support) and kept nowhere; the flow keeps
        /// the public key.
        /// </summary>
        public static (string PrivateKeyPem, string PrivateKeyJwk, ParsedPublicKey PublicKey) NewKeyPair()
        {
            using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var pem = PemEncoding.WriteString("PRIVATE KEY", ec.ExportPkcs8PrivateKey());
            var p = ec.ExportParameters(true);
            var jwk = new JObject
            {
                ["kty"] = "EC",
                ["crv"] = "P-256",
                ["x"] = Base64Url(p.Q.X!),
                ["y"] = Base64Url(p.Q.Y!),
                ["d"] = Base64Url(p.D!),
            }.ToString(Formatting.None);
            CryptographicOperations.ZeroMemory(p.D);
            var spki = ec.ExportSubjectPublicKeyInfo();
            return (pem, jwk, new ParsedPublicKey(spki, "ECDSA P-256", Fingerprint(spki)));
        }

        /// <summary>The public key, as PEM, for the other system's records.</summary>
        public static string PublicKeyPem(byte[] spki) => PemEncoding.WriteString("PUBLIC KEY", spki);

        /// <summary>
        /// A public key from what an admin pasted: PEM ("PUBLIC KEY", "RSA PUBLIC KEY" or a
        /// "CERTIFICATE"), a JWK (EC P-256 or RSA), or base64 of the DER key or certificate. Only
        /// ECDSA P-256 and RSA of 2048 bits or more are taken. A private key is refused (and never
        /// repeated back): it must not be pasted anywhere but the system that signs.
        /// </summary>
        public static ParsedPublicKey? ParsePublicKey(string? text, out string? problem)
        {
            problem = null;
            var t = (text ?? "").Trim();
            if (t.Length == 0)
            {
                problem = "Paste the other system's public key.";
                return null;
            }
            if (t.Length > 20_000)
            {
                problem = "This is too long for a public key.";
                return null;
            }
            if (t.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase))
            {
                problem = PrivateKeyPasted;
                return null;
            }

            byte[]? spki = null;
            try
            {
                if (t.StartsWith('{'))
                    spki = FromJwk(t, out problem);
                else if (t.Contains("-----BEGIN", StringComparison.Ordinal))
                    spki = FromPem(t, out problem);
                else if (FromBase64(t) is { } der)
                    spki = FromDer(der);
                else
                    problem = "This is not a public key: paste it as PEM (-----BEGIN PUBLIC KEY-----), a certificate, a JWK, or base64.";
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException or JsonException or ArgumentException or InvalidOperationException)
            {
                problem = "This could not be read as a public key: " + ex.Message;
                return null;
            }

            if (spki == null)
            {
                problem ??= "This could not be read as a public key.";
                return null;
            }
            var type = KeyType(spki, out problem);
            return type == null ? null : new ParsedPublicKey(spki, type, Fingerprint(spki));
        }

        private const string PrivateKeyPasted = "This is a PRIVATE key. Never paste it here or send it anywhere: it stays on the system that signs. Paste its PUBLIC key.";

        /// <summary>"ECDSA P-256" or "RSA 2048"; null with the reason for any other key.</summary>
        public static string? KeyType(byte[] spki, out string? problem)
        {
            problem = null;
            if (TryEcdsa(spki) is { } ec)
            {
                using (ec)
                {
                    var curve = ec.ExportParameters(false).Curve;
                    if (curve.IsNamed && (curve.Oid.Value == "1.2.840.10045.3.1.7" || string.Equals(curve.Oid.FriendlyName, "nistP256", StringComparison.OrdinalIgnoreCase) || string.Equals(curve.Oid.FriendlyName, "ECDSA_P256", StringComparison.OrdinalIgnoreCase)))
                        return "ECDSA P-256";
                    problem = $"Only ECDSA keys on the curve P-256 (prime256v1) are taken; this one is on {curve.Oid.FriendlyName ?? curve.Oid.Value ?? "another curve"}.";
                    return null;
                }
            }
            if (TryRsa(spki) is { } rsa)
            {
                using (rsa)
                {
                    if (rsa.KeySize < MinRsaBits)
                    {
                        problem = $"This RSA key has {rsa.KeySize} bits; at least {MinRsaBits} are needed.";
                        return null;
                    }
                    if (rsa.KeySize > MaxRsaBits)
                    {
                        problem = $"This RSA key has {rsa.KeySize} bits; at most {MaxRsaBits} are taken.";
                        return null;
                    }
                    return string.Create(CultureInfo.InvariantCulture, $"RSA {rsa.KeySize}");
                }
            }
            problem = "Only ECDSA P-256 and RSA keys are taken.";
            return null;
        }

        public static string Fingerprint(byte[] spki) => Hex(SHA256.HashData(spki));

        /// <summary>A fingerprint's first 16 digits in groups of 4.</summary>
        public static string Short(string fingerprint)
        {
            var all = fingerprint ?? "";
            var f = all.Length >= 16 ? all[..16] : all;
            return string.Join(" ", Enumerable.Range(0, (f.Length + 3) / 4).Select(i => f.Substring(i * 4, Math.Min(4, f.Length - i * 4))));
        }

        private static ECDsa? TryEcdsa(byte[] spki)
        {
            var ec = ECDsa.Create();
            try
            {
                ec.ImportSubjectPublicKeyInfo(spki, out var read);
                if (read == spki.Length)
                    return ec;
            }
            catch (CryptographicException)
            {
            }
            ec.Dispose();
            return null;
        }

        private static RSA? TryRsa(byte[] spki)
        {
            var rsa = RSA.Create();
            try
            {
                rsa.ImportSubjectPublicKeyInfo(spki, out var read);
                if (read == spki.Length)
                    return rsa;
            }
            catch (CryptographicException)
            {
            }
            rsa.Dispose();
            return null;
        }

        private static byte[]? FromPem(string text, out string? problem)
        {
            problem = null;
            var fields = PemEncoding.Find(text);
            var label = text[fields.Label].ToString();
            var der = Convert.FromBase64String(text[fields.Base64Data].ToString());
            switch (label)
            {
                case "PUBLIC KEY":
                    return der;
                case "RSA PUBLIC KEY":
                {
                    using var rsa = RSA.Create();
                    rsa.ImportRSAPublicKey(der, out _);
                    return rsa.ExportSubjectPublicKeyInfo();
                }
                case "CERTIFICATE":
                    return FromCertificate(der);
                default:
                    problem = $"\"{label}\" is not a public key: paste the PUBLIC KEY (or a CERTIFICATE).";
                    return null;
            }
        }

        private static byte[]? FromDer(byte[] der)
        {
            if (TryEcdsa(der) is { } ec)
            {
                ec.Dispose();
                return der;
            }
            if (TryRsa(der) is { } rsa)
            {
                rsa.Dispose();
                return der;
            }
            try
            {
                using var pkcs1 = RSA.Create();
                pkcs1.ImportRSAPublicKey(der, out var read);
                if (read == der.Length)
                    return pkcs1.ExportSubjectPublicKeyInfo();
            }
            catch (CryptographicException)
            {
            }
            return FromCertificate(der);
        }

        private static byte[]? FromCertificate(byte[] der)
        {
            try
            {
                using var cert = X509CertificateLoader.LoadCertificate(der);
                using var ec = cert.GetECDsaPublicKey();
                if (ec != null)
                    return ec.ExportSubjectPublicKeyInfo();
                using var rsa = cert.GetRSAPublicKey();
                return rsa?.ExportSubjectPublicKeyInfo();
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        private static byte[]? FromJwk(string text, out string? problem)
        {
            problem = null;
            var jwk = JsonConvert.DeserializeObject<JToken>(text, JsonRead) as JObject;
            if (jwk == null)
            {
                problem = "This is not a JWK.";
                return null;
            }
            if (jwk["d"] != null || jwk["p"] != null)
            {
                problem = PrivateKeyPasted;
                return null;
            }
            var kty = (string?)jwk["kty"];
            if (kty == "EC")
            {
                if ((string?)jwk["crv"] != "P-256")
                {
                    problem = "Only EC keys on the curve P-256 are taken.";
                    return null;
                }
                using var ec = ECDsa.Create(new ECParameters
                {
                    Curve = ECCurve.NamedCurves.nistP256,
                    Q = new ECPoint { X = FromBase64Url((string?)jwk["x"]), Y = FromBase64Url((string?)jwk["y"]) },
                });
                return ec.ExportSubjectPublicKeyInfo();
            }
            if (kty == "RSA")
            {
                using var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters { Modulus = FromBase64Url((string?)jwk["n"]), Exponent = FromBase64Url((string?)jwk["e"]) });
                return rsa.ExportSubjectPublicKeyInfo();
            }
            problem = "Only EC (P-256) and RSA keys are taken.";
            return null;
        }

        private static byte[]? FromBase64(string text)
        {
            var compact = new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (compact.Length < 40)
                return null;
            try
            {
                return FromBase64Url(compact);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static byte[] FromBase64Url(string? text)
        {
            var t = (text ?? "").Trim().Replace('-', '+').Replace('_', '/');
            if (t.Length == 0)
                throw new FormatException("A part of the key is empty.");
            return Convert.FromBase64String(t.PadRight(t.Length + (4 - t.Length % 4) % 4, '='));
        }

        private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        public static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

        #endregion

        #region Addresses

        /// <summary>
        /// An address ("10.20.30.40", "2001:db8::5") or a range ("10.20.30.0/24"). A range written with
        /// bits after its prefix ("10.20.30.40/24") means the range they belong to, and is written so.
        /// </summary>
        public static bool TryParseAddressRule(string? text, out AddressRule rule, out string? problem)
        {
            rule = default;
            problem = null;
            var t = (text ?? "").Trim();
            var slash = t.IndexOf('/');
            var addressText = slash < 0 ? t : t[..slash];
            // An IPv4 address is written as four numbers (IPAddress also reads "10.20.30" or "010.1.2.3", which mean other addresses).
            if (!IPAddress.TryParse(addressText, out var address) || addressText.Contains('%')
                || (address.AddressFamily == AddressFamily.InterNetwork && address.ToString() != addressText))
            {
                problem = $"\"{t}\" is not an address (like 10.20.30.40) or a range (like 10.20.30.0/24).";
                return false;
            }
            address = Normalize(address)!;
            var max = address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
            var prefix = max;
            if (slash >= 0 && (!int.TryParse(t[(slash + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out prefix) || prefix < 0 || prefix > max))
            {
                problem = $"\"{t}\": the number after / must be 0 to {max}.";
                return false;
            }
            if (prefix < (max == 32 ? 8 : 16))
            {
                problem = $"\"{t}\" is a range of more than {(max == 32 ? "16 million" : "a quarter of all IPv6")} addresses: name the other system's own addresses.";
                return false;
            }
            var bytes = address.GetAddressBytes();
            for (var i = 0; i < bytes.Length; i++)
            {
                var keep = Math.Clamp(prefix - i * 8, 0, 8);
                bytes[i] &= (byte)(0xFF << (8 - keep));
            }
            var network = new IPNetwork(new IPAddress(bytes), prefix);
            rule = new AddressRule(network, prefix == max ? network.BaseAddress.ToString() : network.ToString());
            return true;
        }

        /// <summary>Whether an address is in one of the rules (no rules: every address).</summary>
        public static bool AddressAllowed(IReadOnlyList<AddressRule> rules, IPAddress? address)
        {
            if (rules.Count == 0)
                return true;
            var a = Normalize(address);
            return a != null && rules.Any(r => r.Contains(a));
        }

        /// <summary>The ranges whose CF-Connecting-IP header is believed: the configured list, else Cloudflare's own.</summary>
        public static IReadOnlyList<AddressRule> RulesOf(IEnumerable<string> texts)
        {
            var rules = new List<AddressRule>();
            foreach (var text in texts)
            {
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                // Cloudflare's own ranges are wider than an allow-list may be (a /13): read them without that limit.
                var t = text.Trim();
                var slash = t.IndexOf('/');
                if (IPAddress.TryParse(slash < 0 ? t : t[..slash], out var a)
                    && int.TryParse(slash < 0 ? (a.AddressFamily == AddressFamily.InterNetwork ? "32" : "128") : t[(slash + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var prefix)
                    && prefix >= 0 && prefix <= (a.AddressFamily == AddressFamily.InterNetwork ? 32 : 128))
                {
                    var bytes = Normalize(a)!.GetAddressBytes();
                    var max = bytes.Length * 8;
                    prefix = Math.Min(prefix, max);
                    for (var i = 0; i < bytes.Length; i++)
                        bytes[i] &= (byte)(0xFF << (8 - Math.Clamp(prefix - i * 8, 0, 8)));
                    rules.Add(new AddressRule(new IPNetwork(new IPAddress(bytes), prefix), t));
                }
            }
            return rules;
        }

        /// <summary>
        /// The caller's own address: the address the request came from (the server's forwarded-headers
        /// set-up already took the local proxy's hop), or, when that is one of Cloudflare's, the
        /// address Cloudflare names in CF-Connecting-IP.
        /// </summary>
        public static IPAddress? CallerAddress(IPAddress? peer, string? cloudflareHeader, IReadOnlyList<AddressRule> cloudflare, out bool viaCloudflare)
        {
            viaCloudflare = false;
            var p = Normalize(peer);
            if (p != null && cloudflare.Any(r => r.Contains(p)) && IPAddress.TryParse((cloudflareHeader ?? "").Trim(), out var real))
            {
                viaCloudflare = true;
                return Normalize(real);
            }
            return p;
        }

        /// <summary>An IPv4 address written the IPv6 way (::ffff:10.1.2.3) as IPv4; scope ids dropped.</summary>
        public static IPAddress? Normalize(IPAddress? address)
        {
            if (address == null)
                return null;
            if (address.IsIPv4MappedToIPv6)
                return address.MapToIPv4();
            if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.ScopeId != 0)
                return new IPAddress(address.GetAddressBytes());
            return address;
        }

        #endregion
    }
}
