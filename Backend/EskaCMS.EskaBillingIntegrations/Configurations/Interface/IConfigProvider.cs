using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Configurations.Interface
{
    public interface IConfigProvider
    {
        string GetValue(string val);
    }
}
