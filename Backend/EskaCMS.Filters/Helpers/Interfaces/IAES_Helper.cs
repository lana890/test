using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Filters.Helpers.Interfaces
{
    public interface IAES_Helper
    {
        Task<string> GenerateKey();
        string GenerateRandomIV();
        string Encrypt(string Key, string IV, string plainText);
        string Decrypt(string Key, string IV, string CipherText);
    }
}
