using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace EskaCMS.OTP.Helpers
{
    public interface IRSA_Helper
    {
        string Decrypt(string encrypted);
    }

    public class RSA_Helper : IRSA_Helper
    {
        private readonly RSACryptoServiceProvider _privateKey;
        private readonly RSACryptoServiceProvider _publicKey;
        private static readonly string public_pem = Path.Combine(AppContext.BaseDirectory, "wwwroot", "Keys", "Public.pub.pem");
        private static readonly string private_pem = Path.Combine(AppContext.BaseDirectory, "wwwroot", "Keys", "Private.key.pem");

        public RSA_Helper()
        {
            _publicKey = GetPublicKeyFromPemFile(public_pem);
            _privateKey = GetPrivateKeyFromPemFile(private_pem);
        }

        private RSACryptoServiceProvider GetPrivateKeyFromPemFile(string filePath)
        {
            using (TextReader privateKeyTextReader = new StringReader(File.ReadAllText(filePath)))
            {
                AsymmetricCipherKeyPair readKeyPair = (AsymmetricCipherKeyPair)new PemReader(privateKeyTextReader).ReadObject();

                RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters)readKeyPair.Private);
                RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
                csp.ImportParameters(rsaParams);
                return csp;
            }
        }

        private RSACryptoServiceProvider GetPublicKeyFromPemFile(string filePath)
        {
            using (TextReader publicKeyTextReader = new StringReader(File.ReadAllText(filePath)))
            {
                RsaKeyParameters publicKeyParam = (RsaKeyParameters)new PemReader(publicKeyTextReader).ReadObject();

                RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaKeyParameters)publicKeyParam);

                RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
                csp.ImportParameters(rsaParams);
                return csp;
            }
        }

        public string Decrypt(string encrypted)
        {
            var decryptedBytes = _privateKey.Decrypt(Convert.FromBase64String(encrypted), false);
            return Encoding.UTF8.GetString(decryptedBytes, 0, decryptedBytes.Length);
        }
    }
    public class AES_Helper
    {
        public string GenerateRandomIV()
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.GenerateIV();
                return Convert.ToBase64String(aesAlg.IV);
            }
        }

        public string Decrypt(string Key, string IV, string CipherText)
        {
            byte[] encryptedData = Convert.FromBase64String(CipherText);
            byte[] aesKey = Convert.FromBase64String(Key);
            byte[] iv = Convert.FromBase64String(IV);
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = aesKey;
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(encryptedData))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        public string Encrypt(string Key, string IV, string plainText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Convert.FromBase64String(Key);
                aesAlg.IV = Convert.FromBase64String(IV);
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
    }

    public interface IHashingHelper
    {
        string GetHash(string Source, string Key = HashingHelper.HashingSalt);
    }
    public class HashingHelper : IHashingHelper
    {
        public const string HashingSalt = "SeCureKEyFoRHasHinGEskaRad123";

        public string GetHash(string Source, string Key = HashingSalt)
        {
            Source += Key;
            StringBuilder Sb = new StringBuilder();
            using (SHA256 hash = SHA256.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(Source));
                foreach (byte b in result)
                    Sb.Append(b.ToString("x2"));
            }
            return Sb.ToString();
        }
    }
    public static class ExceptionHelper
    {
        public static Exception ThrowDeepException(Exception Exc)
        {
            if (Exc == null)
            {
                return null;
            }
            else if (Exc.InnerException != null)
            {
                return ThrowDeepException(Exc.InnerException);
            }
            else
            {
                return Exc;
            }
        }
    }
    public static class DateHelper
    {
        public static bool IsCurrentDay(DateTimeOffset DateTime) => DateTime.Date == DateTimeOffset.Now.Date;

        public static bool IsPassedAYear(DateTimeOffset DateTime) => DateTime.AddYears(1).CompareTo(DateTimeOffset.Now) < 0;

        public static bool IsPassedAMonth(DateTimeOffset DateTime) => DateTime.AddMonths(1).CompareTo(DateTimeOffset.Now) < 0;

        public static bool IsPassedAWeek(DateTimeOffset DateTime) => DateTime.AddDays(7).CompareTo(DateTimeOffset.Now) < 0;

        public static bool IsPassedADay(DateTimeOffset DateTime) => DateTime.AddDays(1).CompareTo(DateTimeOffset.Now) < 0;

        public static bool IsPassedTimeSpan(DateTimeOffset DateTime, TimeSpan timeSpan) => DateTime.Add(timeSpan).CompareTo(DateTimeOffset.Now) < 0;

        public static bool IsDayOfWeek(DateTimeOffset DateTime, DayOfWeek Day) => DateTime.DayOfWeek == Day;

        public static bool IsMonthOfYear(DateTimeOffset DateTime, int Month) => DateTime.Month == Month;

        public static bool IsWeekOfMonth(DateTimeOffset DateTime, int WeeekNumber) => (Math.Floor(DateTime.Day / 7.0)) == WeeekNumber;
    }
}
