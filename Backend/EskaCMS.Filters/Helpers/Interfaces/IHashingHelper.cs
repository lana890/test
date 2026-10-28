using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Filters.Helpers.Interfaces
{
    public interface IHashingHelper
    {
        string GetHash(string Source , string Key="");

        bool IsValidHash(string Source ,string Signature , string Key="");
    }
}
