using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Filters.Helpers.Interfaces
{
    public interface IRSA_Helper
    {
        string Decrypt(string encrypted);
        string Encrypt(string text);
        string Decrypt(string encrypted,bool DecodeFirst=false);
        string Encrypt(string text,bool EncodeAfter =false);
        string Encrypt(string PlainText, string PublicKey);
        List<string> EncryptToStringList(string text);
        List<string> EncryptToStringList(string text, string Key);
        string DecryptStringList(List<string> CipherList);
        string getPublicKey();
    }
}
