using System;

using System.IO;
using System.Security.Cryptography;
using System.Text;
using EskaCMS.Filters.Helpers.Interfaces;

namespace EskaCMS.Filters.Helpers
{
    public class HashingHelper : IHashingHelper
    {
        private const string HashingSalt = "SeCureKEyFoRHasHinGEskaRad123";
       

        public bool IsValidHash(string Source,  string Signature, string Key = HashingSalt)
        {
            return Signature == GetHash(Source, Key);
        }

        public string GetHash(string Source, string Key= HashingSalt)
        {
            Source += Key;

            StringBuilder Sb = new StringBuilder();

            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                Byte[] result = hash.ComputeHash(enc.GetBytes(Source));

                foreach (Byte b in result)
                    Sb.Append(b.ToString("x2"));
            }
            var HashedCode = Sb.ToString();
            return HashedCode;
        }
    }
}