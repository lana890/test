using System;

using System.IO;
using System.Security.Cryptography;
using System.Text;
using EskaCMS.Filters.Helpers.Interfaces;

namespace EskaCMS.Filters.Helpers
{
    public class AES_Helper : IAES_Helper
    {
        public async Task<string> GenerateKey()
        {
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();
                //IV = Convert.ToBase64String(aes.IV);
                aes.GenerateKey();
                var key = Convert.ToBase64String(aes.Key);
                //AESKey = key;


                return key;

            }
        }


        public string GenerateRandomIV()
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.GenerateIV();
                return Convert.ToBase64String(aesAlg.IV);
            }
        }
        public string Decrypt(string Key,string IV ,string CipherText)
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
}