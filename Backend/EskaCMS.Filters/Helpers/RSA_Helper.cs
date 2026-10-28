using System;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using EskaCMS.Filters.Helpers.Interfaces;
using System.Text.Encodings.Web;
using System.Web;
using System.Net.Http.Headers;
namespace EskaCMS.Filters.Helpers
{
    public class RSA_Helper : IRSA_Helper
    {
        private readonly RSACryptoServiceProvider _privateKey;
        private readonly RSACryptoServiceProvider _publicKey;

        string public_pem = @"wwwroot\Keys\Public.pub.pem";
        string private_pem = @"wwwroot\Keys\Private.key.pem";

        private double ChunkSize = 70.0;
        public RSA_Helper()
        {
            //@"~\Keys\
            //C:\TFS\Releases\4.2.1\Backend\EskaCMS.WebHost\wwwroot\Keys
            

            _publicKey = GetPublicKeyFromPemFile(public_pem);
            _privateKey = GetPrivateKeyFromPemFile(private_pem);

        }

        public string ModifiyPublicKey(string key)
        {
            return  "-----BEGIN PUBLIC KEY-----\r" + key + "\r-----END PUBLIC KEY-----";
        }

        public string getPublicKey()
        {
            return File.ReadAllText(public_pem);
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

        private RSACryptoServiceProvider GetPublicKeyFromPemFile(String filePath)
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



        public string Encrypt(string text)
        {
            if (text == null) text = "";
            var encryptedBytes = _publicKey.Encrypt(Encoding.UTF8.GetBytes(text), false);
            return Convert.ToBase64String(encryptedBytes);

             
        }

        public string Decrypt(string encrypted, bool DecodeFirst )
        {
            if (DecodeFirst)
            {
                encrypted = HttpUtility.UrlDecode(encrypted);
            }
          
            return Decrypt(encrypted);
        }



        public string Encrypt(string text,bool ThenIncode=false)
        {
            string encrypted= Encrypt(text);

            return ThenIncode ? HttpUtility.UrlEncode(encrypted) : encrypted;
        }

        public string Encrypt(string text , string Key)
        {
            string UserPublicKey = ModifiyPublicKey(Key);
            TextReader publicKeyTextReader = new StringReader(UserPublicKey);

            RsaKeyParameters publicKeyParam = (RsaKeyParameters)new PemReader(publicKeyTextReader).ReadObject();

            RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaKeyParameters)publicKeyParam);

            RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
            //commented to test Go Desc 2 Case if it was because of the encryption or if this replacement wasn't enough
            //// Sondos Suggested FIx for GO Issue -- Added By Israa
            //text = text.Replace("/ \\u{ 20}/ u", "");



            csp.ImportParameters(rsaParams);
            var utf = Encoding.UTF8.GetBytes(text);
            var encryptedBytes = csp.Encrypt(utf, false);
            return Convert.ToBase64String(encryptedBytes);
        }



        public List<string> EncryptToStringList(string text)
        {
            var start = 0;
            var arrayOfEncryptedValues = new List<string>();
            int length = text.Length;

            for (var i = 1; i <= Math.Ceiling(length / ChunkSize); i++)
            {
                var end = (int)ChunkSize * i;
                if (end <= length)
                {
                    arrayOfEncryptedValues.Add(this.Encrypt(text.Substring(start , (end- start )))
                    );
                    start = end;
                }
                else
                {
                    arrayOfEncryptedValues.Add(this.Encrypt(text.Substring(start , (length - start))));
                    break;
                }
            }

            return arrayOfEncryptedValues;
        }

        public List<string> EncryptToStringList(string text, string Key)
        {
            var start = 0;
            var arrayOfEncryptedValues = new List<string>();
            int length = text.Length;

            for (var i = 1; i <= Math.Ceiling(length / ChunkSize); i++)
            {
                var end = (int)ChunkSize * i;
                if (end <= length)
                {
                    arrayOfEncryptedValues.Add(this.Encrypt(text.Substring(start , (end - start )), Key)
                    );
                    start = end;
                }
                else
                {
                    arrayOfEncryptedValues.Add(this.Encrypt(text.Substring(start, (length - start )), Key));
                    break;
                }
            }

            return arrayOfEncryptedValues;
        }

        
        public string DecryptStringList(List<string>CipherList)
        {
            string Reuslt = "";

            foreach (var Cipher in CipherList)
            {
                Reuslt += Decrypt(Cipher);
            }
            return Reuslt;
        }






    }
}