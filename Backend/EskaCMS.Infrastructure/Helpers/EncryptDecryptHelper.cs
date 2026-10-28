using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Infrastructure.Helpers
{
    public class EncryptDecryptHelper
    {
        protected static byte[] key = { 167, 45, 20, 187, 124, 15, 97, 27, 103, 193, 40, 157, 225, 128, 65, 137, 157, 93, 26, 123, 169, 96, 183, 68, 173, 83, 84, 5, 126, 117, 30, 246 };
        protected static byte[] IV = { 166, 220, 28, 159, 17, 192, 129, 215, 189, 102, 122, 194, 199, 44, 154, 62 };
        public static string Encrypt(string pDataToEncrypt)
        {

            RijndaelManaged myRijndael = new RijndaelManaged();

            ICryptoTransform encryptor = myRijndael.CreateEncryptor(key, IV);

            MemoryStream msEncrypt = new MemoryStream();
            CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

            byte[] toEncrypt = Encoding.UTF8.GetBytes(pDataToEncrypt);

            csEncrypt.Write(toEncrypt, 0, toEncrypt.Length);
            csEncrypt.FlushFinalBlock();

            return Convert.ToBase64String(msEncrypt.GetBuffer(), 0, (int)msEncrypt.Length);
        }
        public static string Decrypt(string pDataToDecrypt)
        {
            RijndaelManaged myRijndael = new RijndaelManaged();

            byte[] fromEncrypt = Convert.FromBase64String(pDataToDecrypt);
            ICryptoTransform decryptor = myRijndael.CreateDecryptor(key, IV);

            using (MemoryStream msDecrypt = new MemoryStream(fromEncrypt))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }
      


    }
}
